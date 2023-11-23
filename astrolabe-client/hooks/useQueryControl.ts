import {
  type Control,
  useControl,
  useValueChangeEffect,
} from "@react-typed-forms/core";
import { useEffect } from "react";
import shallowEqual from "shallowequal";
import { NavigationService, useNavigationService } from "../service/navigation";
import { ParsedUrlQuery } from "querystring";
import { stringify } from "querystring";

/**
 * A hook that returns a query control object that can be used to manage the query parameters of the current URL.
 * @returns {Control<typeof ParsedUrlQuery>} The query control object.
 */
export function useQueryControl(): Control<ParsedUrlQuery> {
  const router = useNavigationService();
  const parsedQuery = router.query;
  const queryControl = useControl(parsedQuery, { equals: shallowEqual });

  useEffect(() => {
    if (router.isReady) {
      queryControl.value = router.query;
    }
  }, [router.isReady]);

  useValueChangeEffect(
    queryControl,
    (q) => {
      router.replace(router.pathname + "?" + stringify(q));
    },
    200,
  );

  return queryControl;
}
