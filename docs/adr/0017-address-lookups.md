# ADR 0017 — Address lookups: countries, governorates and cities

Status: accepted (2026-10-29), on `feature/purchasing`. The owner asked for supplier country, governorate and city to be
linked drop-down lists backed by lookup tables (pick a country, see its governorates; pick a governorate, see its
cities), and chose: every country plus Egypt's governorates and cities seeded; governorate and city always required in
an address; managed from screens with a permission; the frontend built in this repository's session, not by Lovable.

## Decision
1. **Owner: Organization.** Countries, governorates and cities are company-wide reference data that suppliers, later
   customers, branches and warehouses all need, so they live in Organization (`Features/Geography`), not in Purchasing.
   Other modules use `Organization.Contracts.IGeographyDirectory` (`FindCountryAsync`, `FindCountriesAsync`,
   `FindCitiesAsync` → `CityInfo` with its governorate and country and `IsUsable`). Additive: nothing existing in
   Organization changed.
2. **Model.** `organization.Countries` (ISO 3166 alpha-2 `Code`, the alternate key `AK_Countries_Code`, fixed once
   created), `Governorates` (country fixed), `Cities` (governorate fixed); Arabic name unique under the parent
   (`UX_Countries_NameAr`, `UX_Governorates_CountryId_NameAr`, `UX_Cities_GovernorateId_NameAr`); temporal master data
   like the rest of Organization. Deactivating a parent leaves the children's flags alone; nothing under an inactive
   parent is offered for new addresses.
3. **API.** `GET /api/organization/countries`, `/governorates?countryCode=`, `/cities?governorateId=` return whole lists
   ordered by Arabic name to any signed-in user (every address form needs them). Create, rename, activate and deactivate
   need the new permission `organization.geography.manage` and company-wide access, like other structure changes.
4. **Supplier addresses store `CityId` only.** The governorate and country are the city's, so they can't disagree; the
   API returns all three with Arabic and English names. `Suppliers.CountryCode` and `SupplierAddresses.CityId` have
   foreign keys to the lookups (restrict). A new country or city must be usable; one the supplier already has may stay
   after it was deactivated, so existing suppliers remain editable (`Purchasing.Supplier.CountryNotFound`,
   `CityNotFound`, 422, "Address n: …").
5. **Seed in `Migration08_Geography`**, as plain SQL, not `HasData`: 249 ISO 3166-1 countries, Egypt's 27 governorates
   and 271 cities and districts. They are editable master data; `HasData` would tie them to the model.
6. **Conversion of existing addresses.** Each address and history version is matched to exactly one seeded city of its
   country by Arabic or English name (and governorate when it had a region). An unknown supplier country, or an address
   with no or several matches, stops the migration with the list and changes nothing. Down writes the names back as
   text. Tested on a throwaway database (both stops, conversion, Down) and applied to `WarehouseERP_Dev` after a backup.

## Consequences
- Address forms use searchable cascading pickers (`CountryPicker`, `GovernoratePicker`, `CityPicker`); the lookups page
  is `/organization/geography`.
- City lists outside Egypt start empty: an administrator adds a country's governorates and cities before suppliers there
  can be saved (the decision "always required").
- Not done: postal-code rules per country, districts below cities, importing official city lists.
