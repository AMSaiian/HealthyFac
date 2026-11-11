# Demo Ingest Seed

This folder holds a snapshot of one real ingest of three demo documents. A development seed reset loads it, so the demo starts with ingested files, chunks with embeddings, and pending drafts. No Docling, no GPU, and no AI call is needed.

## Documents

The three documents in `files/` are fictional demo data. The products, the companies, and the numbers do not exist.

| File | Product | Seeded for |
| --- | --- | --- |
| `morvane-mtc-420-manual.pdf` | A CNC turning center | Seed machine 1 (`0190f3a0-0000-7000-8000-000000000101`) |
| `halvik-ep-80-200-iom.docx` | A centrifugal pump | A company file with a new machine |
| `norrlund-rsv-37-service-data.xlsx` | A screw compressor | A company file with a new machine |

## Files

| File | Service | Content |
| --- | --- | --- |
| `document.json` | Document | The three file rows. |
| `insights.json` | Insights | The attempts, the ingested documents, and the chunks (without vectors). |
| `insights-embeddings.json` | Insights | One vector for each chunk: base64 of 1536 little-endian float32 values. |
| `core.json` | Core | The machine, metric, and status drafts. All drafts are pending. |

Each JSON file holds `referenceTime`: the newest finish time of the attempts. The seeders move every business timestamp by one offset, so the newest attempt finished 1 hour before the seed time.

The files hold no company ID, no user ID, and no audit value. The seeders take the company and the supervisor from `Seeding:DevelopmentData:TestTenant`. The other IDs are the snapshot IDs, so the references between the services stay valid.

## Origin

One real ingest on a verify slot on 2026-11-10: Docling for the conversion, `gemini-3.8-flash` for the extraction, and `gemini-embedding-001` for the vectors. The three files were uploaded by the test supervisor. The PDF was tied to seed machine 1 before the ingest.

## Refresh

Repeat these steps when a migration renames a column or the embedding model changes.

1.  Start a fresh verify slot (`pwsh scripts/verify-stack.ps1 up`).
2.  Upload the three files as the test supervisor.
3.  Tie the PDF to seed machine 1.
4.  Ingest each file.
5.  Export these tables as JSON with the database column names: `file_refs`, `ingest_attempts`, `ingested_documents`, `document_chunks`, `machine_drafts`, `metric_definition_drafts`, and `status_definition_drafts`.
6.  Write the vectors of the chunks into `insights-embeddings.json`, keyed by chunk ID.
7.  Remove `company_id` and every user and audit column (`created_by`, `updated_by`, `deleted_by`, `created_at`, `updated_at`, `deleted_at`).
8.  Set `trace_parent` to null. Write `sha256` as plain hex.
9.  Add `referenceTime` to each file.
10. Replace the files in `files/`.

`DemoIngestSeedFilesTests` checks the references between the files.
