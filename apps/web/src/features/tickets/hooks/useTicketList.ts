import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  slaStatus: undefined,
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  query?: TicketFilters;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState(filters.search);
  const [state, setState] = useState<State>({});
  const [isReloading, setIsReloading] = useState(false);
  const requestId = useRef(0);

  const query = useMemo<TicketFilters>(
    () => ({
      search: debouncedSearch,
      status: filters.status,
      priority: filters.priority,
      categoryId: filters.categoryId,
      customerId: filters.customerId,
      assignedAgentId: filters.assignedAgentId,
      slaStatus: filters.slaStatus,
      unassignedOnly: filters.unassignedOnly,
      page: filters.page,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
    }),
    [
      debouncedSearch,
      filters.status,
      filters.priority,
      filters.categoryId,
      filters.customerId,
      filters.assignedAgentId,
      filters.slaStatus,
      filters.unassignedOnly,
      filters.page,
      filters.pageSize,
      filters.sortBy,
      filters.sortDirection,
    ],
  );

  const load = useCallback(async (showLoading = false) => {
    const currentRequestId = ++requestId.current;
    if (showLoading) setIsReloading(true);

    try {
      const data = await ticketsApi.getTickets(query);
      if (currentRequestId === requestId.current) setState({ query, data, error: undefined });
    } catch (error) {
      const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
      if (currentRequestId === requestId.current) {
        setState((current) => ({ ...current, query, error: message }));
      }
    } finally {
      if (currentRequestId === requestId.current) setIsReloading(false);
    }
  }, [query]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(filters.search), DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [filters.search]);

  useEffect(() => {
    const currentRequestId = ++requestId.current;

    const fetchTickets = async () => {
      try {
        const data = await ticketsApi.getTickets(query);
        if (currentRequestId === requestId.current) setState({ query, data, error: undefined });
      } catch (error) {
        const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
        if (currentRequestId === requestId.current) {
          setState((current) => ({ ...current, query, error: message }));
        }
      } finally {
        if (currentRequestId === requestId.current) setIsReloading(false);
      }
    };

    void fetchTickets();
  }, [query]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => setFilters(emptyFilters);

  const reload = useCallback(() => load(true), [load]);
  const isLoading = isReloading || state.query !== query;

  return { filters, updateFilters, resetFilters, reload, isLoading, data: state.data, error: state.error };
}
