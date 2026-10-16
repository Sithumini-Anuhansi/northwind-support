import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { TicketDetail } from '../../../types/api';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    escalateTicket: vi.fn(),
  },
}));

const escalateTicket = vi.mocked(ticketsApi.escalateTicket);

const ticket: TicketDetail = {
  id: 42,
  reference: 'TCK-0042',
  title: 'Invoice question',
  description: 'A customer needs help with an invoice.',
  status: 'Open',
  priority: 'High',
  customer: {
    id: 2,
    name: 'Fabrikam',
    email: 'help@fabrikam.example',
    phone: null,
    tier: 'Standard',
  },
  category: { id: 3, name: 'Billing', requiresSpecialist: true },
  assignedAgent: { id: 4, fullName: 'Sara Lindqvist' },
  createdAtUtc: '2026-10-16T09:00:00Z',
  updatedAtUtc: '2026-10-16T09:00:00Z',
  dueAtUtc: '2026-10-16T17:00:00Z',
  resolvedAtUtc: null,
  slaStatus: 'WithinSla',
};

describe('EscalationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('blocks a short reason without calling the API', () => {
    render(
      <EscalationPanel
        ticket={ticket}
        onTicketUpdated={vi.fn()}
        onRefresh={vi.fn().mockResolvedValue(undefined)}
      />,
    );

    fireEvent.change(screen.getByLabelText('Reason for escalation'), { target: { value: 'bad' } });
    fireEvent.click(screen.getByRole('button', { name: 'Escalate ticket' }));

    expect(screen.getByText('Enter a reason between 5 and 500 characters.')).toBeInTheDocument();
    expect(escalateTicket).not.toHaveBeenCalled();
  });

  it('shows a 409 response and leaves the ticket unchanged', async () => {
    escalateTicket.mockRejectedValue(new ApiError(409, 'A Resolved ticket cannot be escalated.'));
    const onTicketUpdated = vi.fn();
    const onRefresh = vi.fn().mockResolvedValue(undefined);

    render(<EscalationPanel ticket={ticket} onTicketUpdated={onTicketUpdated} onRefresh={onRefresh} />);
    fireEvent.change(screen.getByLabelText('Reason for escalation'), {
      target: { value: 'Customer impact has increased' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Escalate ticket' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('A Resolved ticket cannot be escalated.');
    expect(onTicketUpdated).not.toHaveBeenCalled();
    expect(onRefresh).not.toHaveBeenCalled();
  });
});
