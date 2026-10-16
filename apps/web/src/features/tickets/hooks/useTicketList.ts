import { useEffect, useMemo, useState } from 'react';
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
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  isLoading: boolean;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState(filters.search);
  const [state, setState] = useState<State>({ isLoading: true });

  const query = useMemo<TicketFilters>(
    () => ({
      search: debouncedSearch,
      status: filters.status,
      priority: filters.priority,
      categoryId: filters.categoryId,
      customerId: filters.customerId,
      assignedAgentId: filters.assignedAgentId,
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
      filters.unassignedOnly,
      filters.page,
      filters.pageSize,
      filters.sortBy,
      filters.sortDirection,
    ],
  );

  const load = async () => {
    setState((current) => ({ ...current, isLoading: true, error: undefined }));

    try {
      const data = await ticketsApi.getTickets(query);
      setState({ isLoading: false, data, error: undefined });
    } catch (error) {
      const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
      setState((current) => ({ ...current, isLoading: false, error: message }));
    }
  };

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(filters.search), DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [filters.search]);

  useEffect(() => {
    void load();
    // query is memoized from every request filter, with only search debounced above.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => setFilters(emptyFilters);

  return { filters, updateFilters, resetFilters, reload: load, ...state };
}
