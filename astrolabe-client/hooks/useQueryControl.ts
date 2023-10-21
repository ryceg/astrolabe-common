import {
  type Control,
  useControl,
  useValueChangeEffect,
} from "@react-typed-forms/core";
import { useEffect } from "react";
import shallowEqual from "shallowequal";
import { NavigationService, useNavigationService } from "../service/navigation";

/**
 * A hook that returns a query control object that can be used to manage the query parameters of the current URL.
 * @returns {Control<typeof URLSearchParams>} The query control object.
 */
export function useQueryControl(): Control<URLSearchParams> {
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
      // if there's no query, we want to remove the query string from the URL
      if (Object.values(q).some((arg) => !!arg?.length)) {
        router.replace(router.pathname + "?" + q.toString());
      } else {
        router.push(router.pathname);
      }
    },
    200,
  );

  return queryControl;
}
