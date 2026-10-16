import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SlaIndicator } from './SlaIndicator';

describe('SlaIndicator', () => {
  it.each([
    ['WithinSla', 'Within SLA'],
    ['AtRisk', 'At risk'],
    ['Breached', 'Breached'],
    ['Met', 'Met'],
    ['NotApplicable', 'No SLA'],
  ] as const)('labels %s as %s', (status, label) => {
    render(<SlaIndicator status={status} />);
    expect(screen.getByText(label)).toBeInTheDocument();
  });
});
