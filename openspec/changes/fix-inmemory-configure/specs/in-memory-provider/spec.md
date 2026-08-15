## MODIFIED Requirements

### Requirement: Options integration for exchange configuration

Registration that accepts a configure callback MUST bind options (capacity, full-mode, etc.) so the callback is not a no-op. Options registration MUST include `IOptions` / `AddOptions` plumbing required for activation.

#### Scenario: Configure capacity

- **WHEN** the consumer passes a configure action that sets channel capacity
- **THEN** the running exchange uses that capacity

#### Scenario: Configure is not a no-op

- **WHEN** two hosts register with different capacity values
- **THEN** their exchanges do not silently share a single hard-coded capacity ignoring both callbacks
