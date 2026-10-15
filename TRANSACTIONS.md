# Transactions and concurrency

## Problem
Concurrent requests can act on stale reads: two attendants could receive the same slot,
and duplicate exit or payment requests could overwrite each other.
Each action calls SaveChanges once, so each is already atomic (implicit transaction).
The gap is isolation, not atomicity.

## Experiments (a temporary Task.Delay widened the race window)
| # | Change | Result |
|---|--------|--------|
| 0 | Baseline | Two cars received the same slot |
| 1 | Explicit transaction, ReadCommitted | Same result: both cars got slot 1 |
| 2 | Explicit transaction, Serializable | One ticket created; the other request was a deadlock victim (SQL error 1205) |
| 3 | Deadlock mapped to 409 in the global handler | Client receives a clean 409 |
| 4 | RowVersion on ParkingTicket (exit, pay) | Before: both requests returned 200 and the second overwrote the first. After: one 200, one 409 |
| 5 | lock in ParkingSpaceTracker | In-memory counts are safe under concurrent requests |

## Design choices
- IssueTicket: requests compete for the same slots, so conflicts are likely. Serializable explicit transaction.
- Exit and Pay: conflicts are rare (double-submit, retry), so optimistic concurrency with RowVersion and no locking.
- The Singleton is outside the database transaction, so it is updated only after SaveChanges succeeds.

## Known limitations
- The frontend shows raw JSON for 409 responses from the global handler.
- Deadlock victims get a 409 and must retry manually; there is no automatic retry.