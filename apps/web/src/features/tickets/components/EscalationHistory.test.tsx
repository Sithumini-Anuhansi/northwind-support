import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { EscalationHistory } from './EscalationHistory';

describe('EscalationHistory', () => {
  it('shows the priority, owner, due date, reason, actor, and timestamp from history', () => {
    render(
      <EscalationHistory
        entries={[
          {
            id: 7,
            fromPriority: 'Medium',
            toPriority: 'High',
            fromAgent: null,
            toAgent: { id: 5, fullName: 'Alex Turner' },
            fromDueAtUtc: null,
            toDueAtUtc: '2026-10-17T12:00:00Z',
            reason: 'Customer impact increased',
            escalatedBy: 'Team Lead',
            escalatedAtUtc: '2026-10-17T04:00:00Z',
          },
        ]}
        isLoading={false}
        onRetry={vi.fn()}
      />,
    );

    expect(screen.getByText('Medium → High')).toBeInTheDocument();
    expect(screen.getByText('Unassigned → Alex Turner')).toBeInTheDocument();
    expect(screen.getByText('Customer impact increased')).toBeInTheDocument();
    expect(screen.getByText('Team Lead')).toBeInTheDocument();
  });
});
