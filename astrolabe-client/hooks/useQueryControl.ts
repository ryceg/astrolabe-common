import {
  useControl,
  useValueChangeEffect,
  type Control,
} from "@react-typed-forms/core";
import { useEffect } from "react";
import shallowEqual from "shallowequal";
import { ParsedUrlQuery } from "querystring";

/**
 * A hook that returns a query control object that can be used to manage the query parameters of the current URL.
 * @returns {Control<typeof ParsedUrlQuery>} The query control object.
 */
export function useQueryControl(
  router: RouterInterface
): Control<ParsedUrlQuery> {
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
        router.replace({ query: q });
      } else {
        router.push(router.pathname);
      }
    },
    200
  );

  return queryControl;
}

interface RouterInterface {
  query: ParsedUrlQuery;
  pathname: string;
  isReady: boolean;
  replace(q: { query: ParsedUrlQuery }): void;
  push(path: string): void;
}
