## MODIFIED Requirements

### Requirement: Empty condition set allows the event

When no conditions are registered for a sector, the sector MUST treat the event as allowed and proceed to the mapper. An empty condition set MUST NOT throw.

#### Scenario: Minimal sector with only a mapper

- **WHEN** a sector is registered with only `AddMapper` and no conditions
- **THEN** each event is mapped and may be exported without throwing due to missing conditions

#### Scenario: No EmptyInstanceBranchException for missing conditions

- **WHEN** the condition instance set is unused (`UseInstance` is false)
- **THEN** sector processing skips condition checkout and does not throw `EmptyInstanceBranchException`
