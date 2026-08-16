# Roadmap → Notifliwy 3.x polish

Status as of 2026-08-16. Goal: ship a **clean, honest 3.x** surface (core + Kafka provider + OTel) without half-finished add-ons.

## Done this wave

- [x] OpenSpec init + baseline specs + P0 change proposals
- [x] #7 empty condition guard
- [x] #8 / #4 InMemory configure + options
- [x] #11 test host `AddOptions`
- [x] #6 OTel meter name
- [x] #13 docs drift
- [x] #10 benchmarks (minimal BDN suite)
- [x] #12 protobuf version align → **then package removed from repo**
- [x] Package icons switched to 1-bit palette (core violet / kafka amber / otel teal)

## Still open (product)

| ID | Item | Notes |
|----|------|--------|
| **#9** | Multi-`WithPipeline` semantics | **Product discussion** — today = chain. Independence would be an API extension, not a hotfix. Defer until you design the 3.x pipeline story. |
| **#2** | Auto mapping | Enhancement; good after #9 decision or as a separate small feature. |

## Pre-3.0 hardening (recommended order)

1. **TFM policy** — net6/net7 are EOL. Decide: keep multi-target for consumers still on 6/7, or drop to `net8.0` only (simpler packages, fewer warnings). Document in README.
2. [x] **MassTransit version pin** — provider + samples on `MassTransit.Kafka` 8.5.10; dropped obsolete `MassTransit.AspNetCore` 7.3.1 from sample.
3. [x] **Kafka sample honesty** — single chained `WithPipeline` (Color → ConstantColor); removed Clear step and independent-pipeline comment.
4. **NuGet unlist / deprecation** for `Synaptix.MassTransit.Kafka.Protobuf` on nuget.org (repo already dropped it). Manual on nuget.org — not automated here.
5. **Public API review** — seal what should be sealed, XML docs on public surface, remove dead types if any.
6. **CI** — ensure `dotnet build` + `dotnet test test/Notifliwy.Units` on PR (if not already).
7. **Version bump plan** — core/provider already at 3.1.0, OTel at 3.0.0. Next publish: align OTel to 3.1.0 or cut a single 3.2.0 after TFM/MassTransit cleanup.

## Explicit non-goals for this polish

- Native Kafka provider rewrite (see `docs/superpowers/specs/2026-05-08-native-kafka-provider-design.md` — design only).
- Independent pipeline fan-out (#9) without a written design.
- AutoMapper / source-gen mapping (#2) without a design.
- net9/net10 targets until 8 is the floor.

## Suggested next session

1. Align MassTransit package versions (kill MSB3277 in samples).
2. Simplify kafka sample to a single `WithPipeline` (or one step list) so it matches chain semantics.
3. OpenSpec change or ADR for #9 when you are ready to talk product.
4. Optional: unlist protobuf package on nuget.org.
