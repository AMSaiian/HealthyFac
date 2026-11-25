# Demo Seed

This folder holds the data of the demo seed. The seed job (`backend/src/Tools/HealthyFac.Seeder`, the Compose service `seeder`) reads it, runs the use cases of the 5 services, and exits. The services start after the job. The demo then holds a company with people, 3 live machines, 3 ingested documents, 3 more pending ingests, a case with its signal, and 13 work orders. No Docling, no GPU, and no AI call is needed.

## Files

| File | Content |
| --- | --- |
| `company.json` | The company (fixed ID, name, time zone, creation moment), the shared `password`, the `people` (key, name, email, role, invite and accept moments), the `openInvites`, and the `shiftOverrides`. |
| `machines.json` | The 3 machines (`pump`, `lathe`, `compressor`): the draft that Core applies, who applies it and when, the go-live date. The `stateChanges` list the state changes of the machines. |
| `work-orders.json` | The 13 work orders with their creation, assignment, and status changes. |
| `telemetry.json` | The script of the telemetry history of mode 2 (see **Telemetry Mode**). Mode 1 ignores it. |
| `cases.json` | The case, its signal, and the work order draft that the planner proposed. |
| `ingest/set-1.json` | The ingest of the 3 documents that belong to the 3 live machines. |
| `ingest/set-2.json` | The ingest of the same 3 documents again. Its drafts stay pending, so apply them to get machines 4 to 6. The last entry of this file is the completion marker (see **Complete and Reset**). |
| `ingest/embeddings.json` | One vector for each chunk: base64 of 1536 little-endian `float32` values, keyed by chunk ID. |
| `files/` | The 3 fictional documents: a CNC turning center manual (PDF), a centrifugal pump manual (DOCX), and a screw compressor data sheet (XLSX). The products and the companies do not exist. |

## Relative Moments

Every moment is relative to the start of the job (the anchor), so the demo always looks current. The job writes no moment in the future.

| Form | Meaning |
| --- | --- |
| `d-14 10:30` | 14 days before the anchor day, at 10:30 in the company time zone. |
| `wd-2 13:00` | 2 workdays (Monday to Friday) before the anchor day, at 13:00. |
| `d-10` | A date without a time (a go-live date). |
| `h-4` | 4 hours before the anchor. |
| `h-N:MM` | N hours and MM minutes before the anchor (`h-22:05`). Only `telemetry.json` uses it. |

`wd` needs a time, and `h` has no time. A moment in the spring clock change that does not exist, a moment without the needed time, and a moment after the anchor stop the job before it writes anything.

## Offsets Inside an Ingest Entry

An entry of an ingest set holds the file (`key`, `path`, `uploadedAt`, `uploadedBy`) and `queuedAt`. The `attempt` holds `startedAfter` and `finishedAfter`: the time (`HH:mm:ss.fff`) after `queuedAt` at which the attempt started and finished. The chunks, the agent runs, and the tool calls keep the IDs of the recorded ingest, so the references between the services stay valid.

## Keys and Fixed IDs

*   A key is a short name of an item in its file (`nadiia`, `pump`, `wear-rings`, `pump-iom`). Events refer to each other by key. A key that no event creates, or that an event uses before its creating event, stops the job with the file, the item, and the rule.
*   The company ID, the machine draft IDs, and the IDs of the ingest entries are fixed GUIDs in the files, so a reset creates the same IDs again.
*   The people use the domain `kalyna-works.test`. The supervisor `nadiia` and the worker `andrii` sign in with the `password` of `company.json`. `scripts/verify-stack.ps1` prints these values.

## Complete and Reset

*   The job checks every file and every request first. A failure names the file, the item, and the rule, and the job writes nothing.
*   The last entry of `ingest/set-2.json` is the completion marker: its machine draft exists in Core only when the whole timeline ran. No event may come after it.
*   A second run of the job on a complete seed prints "The demo seed is complete." and changes nothing. A seed that stopped halfway reads as incomplete: the job exits with code 1 and prints the reset command.
*   `docker compose stop core telemetry document insights auth`, then `docker compose run --rm seeder --reset`, empties the seed data and seeds again with the dates of the new day. The reset deletes the mode-2 marker message first, then the marker draft, then the Keycloak organization with its members, then every blob, then one `TRUNCATE` for each database. The roles of Core and the units of Telemetry stay.

## Telemetry Mode

The job has 2 modes. Mode 1 is the job without an argument: it seeds the data above and no readings. Mode 2 is `--telemetry`: it seeds mode 1 and then the telemetry history of the 3 machines.

| Command | Result |
| --- | --- |
| `docker compose run --rm seeder` | Mode 1. |
| `docker compose run --rm seeder --telemetry` | Mode 2. Run it before `docker compose up`. |
| `docker compose run --rm seeder --reset --telemetry` | Empties the seed data and seeds mode 2 (stop the 5 services first). |

*   **History:** the job simulates each machine with the scenario library `HealthyFac.MachineSimulator.Scenarios` for `windowHours` (48) before the anchor, one sample each `sampleIntervalSeconds` (1). The same intake mapping as the live intake stores the readings in bulk. After each step the job runs the detection (`FleetSilence` and `EvaluateMachine`) on the seed clock, so the episodes and the cases of the history are real.
*   **Fields of `telemetry.json`:** `windowHours`, `sampleIntervalSeconds`, `randomSeed` (the same seed gives the same history), `markerMessageId`, and the lists `drains`, `steps`, `gaps`, and `overrides`. A drain holds `key`, `machine`, `module`, `from`, `drainHours`, its `maintenance` (`by`, `from`, `until`), and an optional `workOrder` (see **Drain work orders**).
*   **Event kinds:** `drain` (a module drains to a failure from `from`), `step` (adds `offset` to one metric from `from` until `until`), `gap` (the machine sends no message), and `override` (replaces values and status codes).
*   **Committed events:** 5 events, each with 1 resolved case: the drain of the lathe spindle with its maintenance, the drain of the pump bearings with its maintenance, a step of the compressor `element.outlet_temperature`, a gap of the pump, and an override of the lathe `spindle.mode` that makes an unknown mode. The open fake case of `cases.json` stays the only open case.
*   **Drain work orders:** a drain may hold a `workOrder` with `title`, `description`, `priority`, `category`, `estimateMinutes`, and `createdAt` (the hour form). The job derives the rest: the key `<drain key>-work-order`, the machine of the drain, `nadiia` as creator at `createdAt` and as assigner 5 minutes later, the person of the maintenance as assignee, `inProgress` at the maintenance start and `completed` at the maintenance end (both by that person). The planned start is the maintenance start on the grid of 5 minutes when the slot lies inside one work block of the assignee and overlaps no other work order of his (the orders of `work-orders.json` count even when they are assigned later), else the earliest grid moment after it with that property. The search starts at the maintenance start and never goes before the assignment. 1 minute before the maintenance start the job seeds the result of the planner agent without an AI call: 1 completed `planner` run of the case with 1 committed `linkWorkOrder` action and 1 succeeded `link_work_order` tool call (the AI log shows it). The open signals of the case get the coverage `linked` with the work order, so the case shows the linked work order. The job stops with the item when the machine has no open case at that moment. Both committed drains have a work order. The 3 other history cases have none.
*   **Marker:** the received message `markerMessageId` is the anchor sample of the last machine. It exists only when the whole history ran.
*   **Run states:**
    *   Empty data: each mode seeds and exits 0.
    *   The mode-1 marker without the mode-2 marker (live messages do not count; also a mode-2 run that stopped inside the history): mode 1 prints "The demo seed is complete." and exits 0. Mode 2 prints that the seed has no complete telemetry history, names `docker compose run --rm seeder --reset --telemetry`, and exits 1.
    *   Both markers: both modes print "The demo seed is complete." and exit 0.
    *   A company without the mode-1 marker: both modes print that the seed is incomplete with the reset command of the mode and exit 1.
*   **Measured:** the committed seed in mode 2 runs 3 min 52 s in the test, and the Telemetry database holds 1418 MB.
*   **Live messages:** the simulator continues from the last seeded counters.
*   **Short threshold episodes:** each drain crosses its warning limits slowly, so the threshold rule opens and resolves many short episodes near a limit (the lathe drain case holds about 94 signals, the pump drain case about 38). Live machines do the same, and the seed keeps it.
*   **Triage runs:** the Insights worker ends the triage run of each resolved history case as `skipped` (reason `caseNotOpen`) with no model call.

## Explicit Seeder Tests

Every test of `HealthyFac.Seeder.UnitTests` and `HealthyFac.Seeder.IntegrationTests` is `Explicit`, so the backend full run skips them (`ExplicitTestsTests` in the architecture tests checks it). Run them with `--explicit only`:

```
dotnet test --project backend/tests/HealthyFac.Seeder.UnitTests --explicit only
dotnet test --project backend/tests/HealthyFac.Seeder.IntegrationTests --explicit only --filter "FullyQualifiedName~FullSeedTests"
```

## Refresh the Ingest Snapshot

Repeat these steps when a migration renames a column, or the embedding model changes. The snapshot is the recorded result of a real ingest.

1.  Start a fresh verify slot (`pwsh scripts/verify-stack.ps1 up`) with the seed job disabled, or reset it.
2.  Upload the three files as the supervisor.
3.  Tie the PDF to a machine of the company.
4.  Ingest each file.
5.  Export the rows of `file_refs`, `ingest_attempts`, `ingested_documents`, `document_chunks`, `agent_runs`, and `agent_tool_calls` (Insights), `machine_drafts` (Core), and the metric, status, and phase drafts with their conditions and limits (Telemetry).
6.  Convert the rows into the entries of `ingest/set-1.json` and `ingest/set-2.json`: write the upload and queue moments in the relative forms, write `startedAfter` and `finishedAfter` as offsets after `queuedAt`, and keep the chunk, run, and tool call IDs.
7.  Write the vectors of the chunks into `ingest/embeddings.json`, keyed by chunk ID.
8.  Remove `company_id` and every user and audit column (`created_by`, `updated_by`, `deleted_by`, `created_at`, `updated_at`, `deleted_at`). Set `trace_parent` to null. Write `sha256` as plain hex.
9.  Replace the files in `files/`.
10. Run `dotnet test --project backend/tests/HealthyFac.Seeder.UnitTests --explicit only` (`CommittedSeedCheckTests` checks the references and the moments on 4 anchor days) and `dotnet test --project backend/tests/HealthyFac.Seeder.IntegrationTests --explicit only --filter "FullyQualifiedName~FullSeedTests"`.
