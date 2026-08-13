# 5-Minute Demo Walkthrough

The app launches straight into a populated database — no setup needed before demoing.

1. **Dashboard** — point out the KPI cards are live queries (not hardcoded), and the four attention-needed panels (recent POs, low-stock products, work orders due soon, pending inspections).
2. **Customers → Vendors** — show search/filter and the "Active only" toggle. Open an existing record to show the edit form and the Deactivate confirmation prompt.
3. **Products → Inventory** — show the product catalog, then switch to Inventory to show the same data reshaped around stock levels, with low-stock rows highlighted amber.
4. **Purchase Orders (main event)** — click **+ New Purchase Order**:
   - Pick a vendor (note the dropdown only offers active vendors)
   - Click **+ Add Line** a couple of times, change the product on one line (watch the unit cost auto-fill), edit a quantity (watch the line total and grand total recalculate live)
   - Save, then reopen it from the list to show the data round-trips correctly
5. **Work Orders** — show the status filter and open a Completed order to show it's locked against edits (an intentional business rule, not a bug).
6. **Serialized Inventory** — show **+ Add Serialized Item**: the work order picker only offers work orders for serialized products, and the serial number is auto-suggested.
7. **Quality Control** — show the Pending/Passed/Failed color coding, tying back to the serialized items from the previous step.

If there's time left, open `docs/ARCHITECTURE.md` and walk through the WinForms → Application → Infrastructure → EF Core → SQL Server layering, or mention the `ComboBox` timing bug from `docs/INTERVIEW_NOTES.md` as a debugging story.
