"use client";

import { useControl, useControlEffect } from "@react-typed-forms/core";
import { useApiClient } from "@astroapps/client";
import { Button } from "@astrolabe/ui/Button";
import {
  TeaRoomClient,
  TeaRoomStatus,
  TeaOrder,
  KettleStatus,
  TeaType,
  BrewedTea,
  OrderStatus,
  BrewingState,
} from "client-common/client";
import { useCallback, useMemo } from "react";
import { createStdFormRenderer } from "@/renderers";
import { TeaOrderForm } from "client-common/formdefs";
import {
  TeaOrderFormSchema,
  defaultTeaOrderForm,
  TeaOrderForm as TeaOrderFormType,
} from "client-common/schemas";
import { RenderFormData } from "@/RenderFormData";

// Parse TimeSpan string format (e.g., "00:00:15.1234567") to seconds
function formatBrewDuration(duration: string): string {
  const parts = duration.split(":");
  if (parts.length >= 3) {
    const hours = parseInt(parts[0], 10);
    const minutes = parseInt(parts[1], 10);
    const seconds = parseFloat(parts[2]);
    const totalSeconds = hours * 3600 + minutes * 60 + seconds;
    return `${Math.round(totalSeconds)}s`;
  }
  return duration;
}

export default function TeaRoomPage() {
  const client = useApiClient(TeaRoomClient);

  const roomStatus = useControl<TeaRoomStatus | null>(null);
  const myOrders = useControl<TeaOrder[]>([]);
  const orderHistory = useControl<TeaOrder[]>([]);
  const teaTypes = useControl<TeaType[]>([]);
  const formData = useControl<TeaOrderFormType>({ ...defaultTeaOrderForm });
  const isOrdering = useControl(false);
  const collectedTeas = useControl<BrewedTea[]>([]);
  const error = useControl<string | null>(null);

  // Create form renderer
  const renderer = useMemo(() => createStdFormRenderer(), []);

  // Fetch tea types and room status on mount
  useControlEffect(
    () => null,
    async () => {
      // Fetch available tea types from backend
      try {
        const types = await client.getTeaTypes();
        teaTypes.value = types;
        if (types.length > 0 && !formData.fields.teaType.value) {
          formData.fields.teaType.value = types[0];
        }
      } catch (e) {
        console.error("Failed to fetch tea types:", e);
      }

      await refreshStatus();
      // Set up polling every second
      const interval = setInterval(refreshStatus, 1000);
      return () => clearInterval(interval);
    },
    true
  );

  const refreshStatus = useCallback(async () => {
    try {
      const status = await client.getDefaultRoomStatus();
      roomStatus.value = status;

      // Update order statuses for tracked orders
      const updatedOrders = await Promise.all(
        myOrders.value.map(async (order) => {
          if (order.status === OrderStatus.Collected || order.status === OrderStatus.Cancelled) {
            return order;
          }
          const updated = await client.checkOrder("main-tea-room", order.id);
          return updated ?? order;
        })
      );
      myOrders.value = updatedOrders;

      // Fetch order history
      const history = await client.getOrderHistory("main-tea-room", 20);
      orderHistory.value = history;
    } catch (e) {
      console.error("Failed to refresh status:", e);
    }
  }, [client, myOrders, roomStatus, orderHistory]);

  const orderTea = useCallback(async () => {
    if (!formData.fields.customerName.value.trim()) {
      error.value = "Please enter your name";
      return;
    }
    if (!formData.fields.teaType.value) {
      error.value = "Please select a tea type";
      return;
    }

    isOrdering.value = true;
    error.value = null;

    try {
      const order = await client.orderTeaDefault({
        teaType: formData.fields.teaType.value,
        customerName: formData.fields.customerName.value.trim(),
      });
      myOrders.value = [...myOrders.value, order];
      await refreshStatus();
    } catch (e) {
      error.value = "Failed to place order. Please try again.";
      console.error("Failed to order tea:", e);
    } finally {
      isOrdering.value = false;
    }
  }, [client, formData, myOrders, isOrdering, error, refreshStatus]);

  const collectOrder = useCallback(
    async (orderId: string) => {
      try {
        const brewedTea = await client.collectOrder("main-tea-room", orderId);
        if (brewedTea) {
          collectedTeas.value = [...collectedTeas.value, brewedTea];
          myOrders.value = myOrders.value.map((o) =>
            o.id === orderId ? { ...o, status: OrderStatus.Collected } : o
          );
        }
      } catch (e) {
        error.value = "Failed to collect tea. It may not be ready yet.";
        console.error("Failed to collect:", e);
      }
    },
    [client, collectedTeas, myOrders, error]
  );

  return (
    <main className="min-h-screen p-8 bg-gradient-to-br from-amber-50 to-orange-50">
      <div className="max-w-6xl mx-auto">
        <h1 className="text-4xl font-bold mb-2 text-amber-900">🍵 Tea Room</h1>
        <p className="text-amber-700 mb-8">
          Orleans-powered tea brewing demonstration
        </p>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Order Panel */}
          <div className="bg-white rounded-xl shadow-lg p-6">
            <h2 className="text-2xl font-semibold mb-4 text-amber-800">
              Order Tea
            </h2>

            <div className="space-y-4">
              <RenderFormData
                data={formData}
                controls={TeaOrderForm.controls}
                schema={TeaOrderFormSchema}
                renderer={renderer}
              />

              {error.value && (
                <p className="text-red-600 text-sm">{error.value}</p>
              )}

              <Button
                onClick={orderTea}
                disabled={isOrdering.value}
                className="w-full bg-amber-600 hover:bg-amber-700 text-white py-2 px-4 rounded-md transition-colors disabled:opacity-50"
              >
                {isOrdering.value ? "Ordering..." : "Order Tea ☕"}
              </Button>
            </div>

            {/* My Orders */}
            <div className="mt-6">
              <h3 className="text-lg font-semibold mb-3 text-amber-800">
                My Orders
              </h3>
              {myOrders.value.length === 0 ? (
                <p className="text-gray-500 text-sm">No orders yet</p>
              ) : (
                <div className="space-y-2">
                  {myOrders.value.map((order) => (
                    <OrderCard
                      key={order.id}
                      order={order}
                      onCollect={() => collectOrder(order.id)}
                    />
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Kettles Status */}
          <div className="bg-white rounded-xl shadow-lg p-6">
            <h2 className="text-2xl font-semibold mb-4 text-amber-800">
              Kettles
            </h2>
            {roomStatus.value ? (
              <div className="space-y-4">
                {roomStatus.value.kettles.map((kettle) => (
                  <KettleCard key={kettle.kettleId} kettle={kettle} />
                ))}
                <div className="pt-4 border-t border-gray-200">
                  <p className="text-sm text-gray-600">
                    <span className="font-medium">Available:</span>{" "}
                    {roomStatus.value.availableKettles} /{" "}
                    {roomStatus.value.kettles.length}
                  </p>
                  <p className="text-sm text-gray-600">
                    <span className="font-medium">Pending Orders:</span>{" "}
                    {roomStatus.value.pendingOrderCount}
                  </p>
                  <p className="text-sm text-gray-600">
                    <span className="font-medium">Completed Today:</span>{" "}
                    {roomStatus.value.completedOrdersToday}
                  </p>
                  {roomStatus.value.mostPopularTeaToday && (
                    <p className="text-sm text-gray-600">
                      <span className="font-medium">Most Popular:</span>{" "}
                      {roomStatus.value.mostPopularTeaToday}
                    </p>
                  )}
                </div>
              </div>
            ) : (
              <p className="text-gray-500">Loading...</p>
            )}
          </div>

          {/* Collected Teas & History */}
          <div className="bg-white rounded-xl shadow-lg p-6">
            <h2 className="text-2xl font-semibold mb-4 text-amber-800">
              Collected Teas
            </h2>
            {collectedTeas.value.length === 0 ? (
              <p className="text-gray-500 text-sm mb-6">
                No teas collected yet
              </p>
            ) : (
              <div className="space-y-3 mb-6">
                {collectedTeas.value.map((tea) => (
                  <div
                    key={tea.sessionId}
                    className="p-3 bg-green-50 rounded-lg border border-green-200"
                  >
                    <p className="font-medium text-green-800">{tea.teaType}</p>
                    <p className="text-sm text-green-600 italic">
                      "{tea.flavorNotes}"
                    </p>
                    <p className="text-xs text-green-500 mt-1">
                      Brewed in {formatBrewDuration(tea.brewDuration)}
                    </p>
                  </div>
                ))}
              </div>
            )}

            <h3 className="text-lg font-semibold mb-3 text-amber-800">
              Recent Orders
            </h3>
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {orderHistory.value.slice(0, 10).map((order) => (
                <div
                  key={order.id}
                  className="text-sm p-2 bg-gray-50 rounded flex justify-between"
                >
                  <span>
                    {order.customerName} - {order.teaType}
                  </span>
                  <StatusBadge status={order.status} />
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* About Orleans */}
        <div className="mt-8 bg-white rounded-xl shadow-lg p-6">
          <h2 className="text-2xl font-semibold mb-4 text-amber-800">
            About This Demo
          </h2>
          <div className="prose prose-amber max-w-none">
            <p>
              This page demonstrates{" "}
              <strong>Microsoft Orleans</strong> - a distributed actor framework
              for building scalable, distributed applications.
            </p>
            <ul className="mt-4 space-y-2">
              <li>
                <strong>Tea Room Grain:</strong> Manages the overall tea room,
                coordinates kettles, and processes orders
              </li>
              <li>
                <strong>Kettle Grains:</strong> Each kettle is an independent
                actor that handles brewing with timers
              </li>
              <li>
                <strong>State Persistence:</strong> All grain state is persisted
                and survives restarts
              </li>
              <li>
                <strong>Grain-to-Grain Communication:</strong> The tea room
                grain communicates with kettle grains
              </li>
            </ul>
          </div>
        </div>
      </div>
    </main>
  );
}

function OrderCard({
  order,
  onCollect,
}: {
  order: TeaOrder;
  onCollect: () => void;
}) {
  return (
    <div className="p-3 bg-amber-50 rounded-lg border border-amber-200">
      <div className="flex justify-between items-start">
        <div>
          <p className="font-medium text-amber-900">{order.teaType}</p>
          <p className="text-xs text-amber-600">
            {order.queuePosition > 0 && order.status === OrderStatus.Pending
              ? `Queue position: ${order.queuePosition}`
              : order.assignedKettleId
              ? `Kettle: ${order.assignedKettleId}`
              : ""}
          </p>
        </div>
        <StatusBadge status={order.status} />
      </div>
      {order.status === OrderStatus.Ready && (
        <Button
          onClick={onCollect}
          className="mt-2 w-full bg-green-600 hover:bg-green-700 text-white py-1 px-3 rounded text-sm"
        >
          Collect Tea
        </Button>
      )}
    </div>
  );
}

function KettleCard({ kettle }: { kettle: KettleStatus }) {
  const getStateColor = (session: KettleStatus["currentSession"]) => {
    if (!session) return "bg-green-100 border-green-300";
    switch (session.state) {
      case BrewingState.Heating:
        return "bg-orange-100 border-orange-300";
      case BrewingState.Steeping:
        return "bg-amber-100 border-amber-300";
      case BrewingState.Ready:
        return "bg-green-100 border-green-300";
      default:
        return "bg-gray-100 border-gray-300";
    }
  };

  return (
    <div className={`p-4 rounded-lg border-2 ${getStateColor(kettle.currentSession)}`}>
      <div className="flex justify-between items-center mb-2">
        <span className="font-medium">{kettle.kettleId}</span>
        {kettle.isAvailable ? (
          <span className="text-xs bg-green-500 text-white px-2 py-1 rounded">
            Available
          </span>
        ) : (
          <span className="text-xs bg-amber-500 text-white px-2 py-1 rounded">
            Busy
          </span>
        )}
      </div>
      {kettle.currentSession && (
        <div className="text-sm">
          <p>
            Brewing: <strong>{kettle.currentSession.teaType}</strong>
          </p>
          <p>For: {kettle.currentSession.orderedBy}</p>
          <p>State: {kettle.currentSession.state}</p>
          {kettle.secondsRemaining > 0 && (
            <p className="text-amber-700">
              ⏱️ {kettle.secondsRemaining}s remaining
            </p>
          )}
        </div>
      )}
      <p className="text-xs text-gray-500 mt-2">
        Brews today: {kettle.totalBrewsToday}
      </p>
    </div>
  );
}

function StatusBadge({ status }: { status: OrderStatus }) {
  const colors: Record<OrderStatus, string> = {
    [OrderStatus.Pending]: "bg-gray-200 text-gray-800",
    [OrderStatus.Brewing]: "bg-amber-200 text-amber-800",
    [OrderStatus.Ready]: "bg-green-200 text-green-800",
    [OrderStatus.Collected]: "bg-blue-200 text-blue-800",
    [OrderStatus.Cancelled]: "bg-red-200 text-red-800",
  };

  return (
    <span className={`text-xs px-2 py-1 rounded ${colors[status]}`}>
      {status}
    </span>
  );
}
