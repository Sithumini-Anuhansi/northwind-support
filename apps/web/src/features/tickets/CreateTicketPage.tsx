import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ErrorMessage } from '../../components/ErrorMessage';
import { Spinner } from '../../components/Spinner';
import { useReferenceData } from '../../hooks/useReferenceData';
import type { TicketDetail } from '../../types/api';
import { formatDateTime } from '../../lib/format';
import { CreateTicketForm } from './components/CreateTicketForm';
import { SlaIndicator } from './components/SlaIndicator';

export function CreateTicketPage() {
  const reference = useReferenceData();
  const navigate = useNavigate();
  const [created, setCreated] = useState<TicketDetail>();

  return (
    <section>
      <header className="page-header">
        <div>
          <p className="breadcrumb">
            <Link to="/tickets">Tickets</Link> / New
          </p>
          <h1>Raise a ticket</h1>
        </div>
      </header>

      {reference.isLoading && <Spinner label="Loading customers and categories" />}

      {reference.error && (
        <ErrorMessage message={reference.error} onRetry={() => void reference.reload()} />
      )}

      {created && (
        <div className="card notice" role="status">
          <p>
            Raised <strong>{created.reference}</strong> for {created.customer.name}.
          </p>
          <h2>Triage result</h2>
          <dl className="ticket-summary__details">
            <dt>Applied priority</dt>
            <dd>{created.priority}</dd>
            <dt>Due</dt>
            <dd>{created.dueAtUtc ? formatDateTime(created.dueAtUtc) : 'No SLA due date'}</dd>
            <dt>SLA status</dt>
            <dd><SlaIndicator status={created.slaStatus} /></dd>
            <dt>Assignment</dt>
            <dd>
              {created.assignedAgent
                ? created.assignedAgent.fullName
                : `Unassigned: ${created.triage?.assignmentReason ?? 'no eligible agent was found.'}`}
            </dd>
            {created.triage && (
              <>
                <dt>Priority decision</dt>
                <dd>{created.triage.priorityReason}</dd>
                <dt>SLA window</dt>
                <dd>{created.triage.slaWindowReason}</dd>
              </>
            )}
          </dl>
          <div className="button-row">
            <button
              type="button"
              className="button button--primary"
              onClick={() => void navigate(`/tickets/${created.id}`)}
            >
              Open {created.reference}
            </button>
            <button type="button" className="button button--ghost" onClick={() => setCreated(undefined)}>
              Raise another
            </button>
          </div>
        </div>
      )}

      {reference.data && !created && (
        <CreateTicketForm
          customers={reference.data.customers}
          categories={reference.data.categories}
          onCreated={setCreated}
        />
      )}
    </section>
  );
}
