# توضيحات مهمة قبل تنفيذ Migration 01

**النطاق:** شركة واحدة، فروع ومخازن متعددة، تجارة وتوزيع فقط. لا توجد كيانات تصنيع.

- الـ `DbContext` واحد، والجداول موزعة منطقيًا بين `organization` و`catalog`.
- كل الجداول الـ 13 هي SQL Server System-Versioned Temporal Tables، وسجل إصداراتها في `history`.
- `WarehouseLocation.IsFrozen` **غير موجود**؛ تجميد الجرد سوف يُستمد من `StockCountScope` النشط عند بناء Inventory لاحقًا.
- `Product.BaseUnitId` هو **المرجع الوحيد** للوحدة الأساسية. لا يوجد `ProductUnit.IsBaseUnit`؛ خدمة إنشاء المنتج تُدرج وحدة الأساس بـ `ConversionFactor=1`.
- `Warehouse.WarehouseType` يحدد Physical/Transit؛ `AllowsSelling` يحدد السماح بالبيع من المخزن الفعلي، مع CHECK يمنع بيع Transit نهائيًا.
- التعريفات البنيوية وحدها لا تفرض **وجود** Location افتراضي ولا **وجود** ProductUnit الأساس على كل المسارات؛ خدمة الإنشاء تنشئهما في نفس `SaveChanges`, لكن يجب منع الكتابة المباشرة أو إضافة قواعد DB أقوى عند الحاجة.
- فهرس `WarehouseLocations(WarehouseId)` فريد **مع فلتر** `IsDefault=1`: يمنع أكثر من Default، ولا يضمن وجود واحد إن حُذف مباشرة.
- البيانات التعريفية تحتفظ بمصدر التعديل (CreatedBy/UpdatedBy) إضافة إلى Temporal؛ لا يوجد AuditLog على تعديلات Master. AuditLog للعمليات التشغيلية في Migration لاحقة.
- `Category.ParentId` يمنع الإشارة إلى الذات مباشرة؛ كشف الدوران الأطول يحتاج Validation تطبيقي.
- تواريخ سريان الضرائب تستخدم `DateOnly` وفترة `[EffectiveFrom, EffectiveTo)`؛ فحص التداخل الزمني يحتاج أمر إدارة بضمان تزامن خاص به.
- لا نضع UnitCost/ValueDelta داخل `StockMovement` مستقبلًا؛ قيم التكلفة في `InventoryCostEntry`. أسعار الوحدة `decimal(19,8)`.
- سياسة `RequiresApproval` لكل DocumentType، والانتقال المباشر من Draft إلى Posted للمستندات القادمة من Modules أخرى، ينفذ في Inventory Migration وليست ضمن Migration 01.
- قيد `ReservedQuantity <= OnHandSellable` **ممنوع**؛ الحجز والصرف المباشر بتحقق ذري، بينما تلف/فقد/جرد قد ينتج OverReserved Alert دون منع التصحيح.

**التقسيم:** المشروع Modular Monolith، وكل Module فيه 4 مشاريع: `.Domain` (القواعد)، و`.Application` (الـ use cases)، والمشروع الخارجي (Infrastructure و Presentation)، و`.Contracts` (الحاجة الوحيدة اللي الـ Modules التانية تشوفها). الـ `ErpDbContext` في `WarehouseERP.Persistence` بيجمع configurations كل الـ Modules، و`WarehouseERP.ArchitectureTests` بيفشل لو Module لمس Module تاني أو طبقة لمست طبقة مش مسموحلها. التفاصيل في `docs/ARCHITECTURE.md` والقرارات في `docs/adr/`.

**الحالة:** Migration 01 اتولّدت بـ `dotnet ef` واتطبقت واتجربت على LocalDB، والاختبارات في `tests/`. أوامر التشغيل في `README.md`.
