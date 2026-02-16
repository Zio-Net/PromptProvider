# PromptProvider Improvement Suggestions

This document captures improvement opportunities focused on current package behavior (prompt fetching + management with Langfuse), without adding new end-user features.

## 1) Performance

1. **Use true batch retrieval for `GetPromptsAsync`**  
   Current implementation fetches prompts sequentially (`foreach` + `await`), which increases latency linearly with prompt count. Use bounded parallelism (`Task.WhenAll` + semaphore) to reduce aggregate wait time and improve throughput.

2. **Add in-memory caching for read paths**  
   `GetPromptAsync` and `GetChatPromptAsync` repeatedly call Langfuse for the same key/version/label combinations. Add optional `IMemoryCache` with short TTL and explicit cache keys to reduce remote calls and improve p95 latency.

3. **Avoid repetitive key/label resolution work**  
   Prompt key lookup happens on each request via dictionary access from options provider. Cache resolved mappings and labels in the provider (or use immutable snapshots) to reduce repeated overhead.

4. **Prefer streaming JSON APIs**  
   Multiple methods call `ReadAsStringAsync` then deserialize. Consider `ReadFromJsonAsync` / `DeserializeAsync` on stream to reduce memory allocations for large payloads.

5. **Tune `HttpClient` for high-throughput**  
   Configure handler settings through `IHttpClientFactory` (`PooledConnectionLifetime`, `MaxConnectionsPerServer`, decompression, timeout policy) for better connection reuse and resilience under load.

## 2) Reliability & Resilience

6. **Introduce retry/backoff + timeout policies**  
   Add resilience pipeline (Polly) for transient failures (429/5xx/network faults), with jittered backoff and per-request timeout to stabilize behavior during Langfuse incidents.

7. **Differentiate fallback conditions**  
   `PromptService` currently falls back to local defaults for broad exceptions. Split handling for not-found, auth errors, and server errors so callers can choose strict vs fallback semantics.

8. **Validate and normalize options at startup**  
   Convert options to validated options (`ValidateOnStart`) with clear failure messages for invalid URL, missing credentials, etc. Fail fast rather than discovering issues at runtime.

9. **Support runtime config refresh safely**  
   `LangfuseService` snapshots options in constructor. If config rotates (e.g., keys), service needs restart. Consider `IOptionsMonitor` or a reconfigurable auth/header strategy.

## 3) API Usability

10. **Make source/type strongly typed**  
    Response models use string literals for source/type (e.g., "Langfuse", "Local", "text", "chat"). Replace with enums/constants to improve discoverability and prevent typos.

11. **Clarify precedence rules in API contracts**  
    Document and enforce precedence between configured label, method label, and version consistently across all methods (including create/update paths).

12. **Add XML docs for all public models/options**  
    Interface methods are documented, but many DTOs/options are not. Expanded XML docs improve IDE guidance and reduce misuse for package consumers.

13. **Return collection interfaces instead of concrete `List<>`**  
    Public APIs returning `IReadOnlyList<T>` are easier to evolve and communicate immutability of results.

## 4) Readability & Maintainability

14. **Remove duplicated mapping/validation logic**  
    Prompt key mapping and request validation are repeated across create/get/update methods. Extract internal helpers to reduce duplication and maintenance risk.

15. **Consolidate prompt retrieval logic**  
    `GetPromptAsync` and `GetChatPromptAsync` are structurally similar. Shared internal template methods can cut repetition while keeping typed outputs.

16. **Use constants for API routes and default label**  
    Route fragments and default label values are currently duplicated in methods. Central constants improve readability and reduce drift.

17. **Tighten model types for external payloads**  
    Several response/request fields are `object?` (`Config`, `ResolutionGraph`). Use typed records or `JsonElement` to improve safety and clarity.

## 5) Packaging & Compatibility

18. **Target stable TFMs for NuGet consumers**  
    Package currently targets `net10.0` only. Consider multi-targeting stable TFMs (e.g., `net8.0`, optionally `netstandard2.1`) to maximize adoption.

19. **Avoid heavy framework reference in library package**  
    Referencing `Microsoft.AspNetCore.App` in a class library can pull in unnecessary runtime surface. Prefer explicit package dependencies only.

20. **Improve NuGet metadata quality**  
    Replace placeholder repository URL and author fields, add icon/readme/package project URL and symbols/source link for better package trust and debugging experience.

## 6) Test Coverage & Quality Gates

21. **Add unit tests for fallback and mapping semantics**  
    Cover logical->actual key mapping, label override behavior, and local fallback paths for both text/chat prompts.

22. **Add HTTP contract tests for Langfuse API integration**  
    Use mocked `HttpMessageHandler` to verify request URIs, query construction, auth header behavior, and deserialization error handling.

23. **Add package-level CI checks**  
    Ensure `dotnet build`, tests, analyzers, and package validation run in CI before publish.

## 7) Observability & Diagnostics

24. **Standardize structured logging events**  
    Introduce event IDs and consistent property names (`PromptKey`, `ActualKey`, `Label`, `Version`) to make logs easier to query.

25. **Add minimal metrics hooks**  
    Track Langfuse request count, failures, fallback count, and latency histograms to monitor operational health.

---

## Suggested Execution Order

1. **Foundation**: options validation + resilience policies + tests for existing behavior.
2. **High-impact performance**: cache + bounded parallel retrieval.
3. **Maintainability**: deduplicate internal logic and constants.
4. **Packaging**: TFM/dependency/metadata cleanup.
5. **Observability**: structured logs + metrics.

