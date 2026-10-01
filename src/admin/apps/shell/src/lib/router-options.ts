/** Remount a route's component when its params change, so state built for one entity is never reused for the next. */
export const remountOnParamChange = ({ params }: { params: unknown }) => params;
