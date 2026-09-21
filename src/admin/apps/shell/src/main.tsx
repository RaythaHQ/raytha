import { ApiError, bootstrapSession } from "@raytha/api";
import { keepPreviousData, MutationCache, QueryCache, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { router } from "./router";
import "./styles/_variables.scss";
import "./styles/_keyframe-animations.scss";
import "./styles.css";

/** Session expired mid-use: send the user back to the login screen. */
function redirectToLoginOn401(error: unknown) {
  if (error instanceof ApiError && error.status === 401) {
    void router.navigate({ to: "/login", replace: true });
  }
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({ onError: redirectToLoginOn401 }),
  mutationCache: new MutationCache({ onError: redirectToLoginOn401 }),
  defaultOptions: {
    queries: {
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status >= 400 && error.status < 500) && failureCount < 1,
      refetchOnWindowFocus: false,
      placeholderData: keepPreviousData,
    },
  },
});

void bootstrapSession().then(() => {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </StrictMode>,
  );
});
