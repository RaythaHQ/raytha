import { useEffect, useRef } from "react";

type FindTarget = () => HTMLElement | null | undefined;

/** Focuses an element after the next render, once a list edit has put it on the page. */
export function usePendingFocus(): (find: FindTarget) => void {
  const pending = useRef<FindTarget | null>(null);
  useEffect(() => {
    const find = pending.current;
    if (find) {
      pending.current = null;
      find()?.focus();
    }
  });
  return (find) => {
    pending.current = find;
  };
}
