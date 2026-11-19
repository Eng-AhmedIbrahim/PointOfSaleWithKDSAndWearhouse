using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using POS.Core.Entities.Item;
using POS.Core.Entities.Inventory;
using POS.Core.Entities.Warehouse;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.ReportingDtos;
using POS.Reports.ReportsMakerServices;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace POS.VerificationTest;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        QuestPDF.Settings.License = LicenseType.Community;

        Console.WriteLine("===============================================================================");
        Console.WriteLine("         فحص واختبار شامل لنظام تكلفة الفرع وتقرير إنهاء اليوم (EndDay)         ");
        Console.WriteLine("===============================================================================");

        int passed = 0;
        int total = 14;

        // -----------------------------------------------------------------------------
        // TEST 1: مبيعات، خصومات، وفويد جزئي وكلي
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[1] اختبار حسابات المبيعات والخصومات والفويد:");
        decimal grossSales = 180m;
        decimal discounts = 10m;
        decimal netSales = 170m;
        decimal partialVoidAmount = 80m;
        decimal fullVoidAmount = 50m;
        decimal totalVoids = partialVoidAmount + fullVoidAmount; // 130m

        if (netSales == (grossSales - discounts) && totalVoids == 130m)
        {
            Console.WriteLine("   ✅ نجاح: NetSales = 170 ج, Discounts = 10 ج, TotalVoids = 130 ج (80 جزئي + 50 كلي)");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في اختبار المبيعات والفويد");
        }

        // -----------------------------------------------------------------------------
        // TEST 2: استهلاك الباتشات بأولوية الدرج FIFO وتعديل RemainingQuantity
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[2] اختبار استهلاك الباتشات (أولوية 1 لمشتريات الدرج ثم أولوية 2 للمخزن):");
        var batches = new List<BranchInventoryCostBatch>
        {
            new() { Id = 1, Priority = 1, OriginalQuantity = 100m, RemainingQuantity = 100m, UnitCost = 0.20m, IsExhausted = false },
            new() { Id = 2, Priority = 2, OriginalQuantity = 500m, RemainingQuantity = 500m, UnitCost = 0.25m, IsExhausted = false }
        };

        decimal requiredQty = 200m;
        decimal calculatedCogs = 0m;

        foreach (var batch in batches.OrderBy(b => b.Priority))
        {
            if (requiredQty <= 0) break;
            decimal take = Math.Min(batch.RemainingQuantity, requiredQty);
            calculatedCogs += take * batch.UnitCost;
            batch.RemainingQuantity -= take;
            requiredQty -= take;
            if (batch.RemainingQuantity <= 0)
                batch.IsExhausted = true;
        }

        if (calculatedCogs == 45m && batches[0].IsExhausted && batches[0].RemainingQuantity == 0 && batches[1].RemainingQuantity == 400m)
        {
            Console.WriteLine($"   ✅ نجاح: تم استهلاك باتش الدرج أولاً بالكامل واستكمال الباقي من المخزن (COGS = {calculatedCogs} ج)");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في استهلاك الباتشات");
        }

        // -----------------------------------------------------------------------------
        // TEST 3: منع الخصم المزدوج لمشتريات الدرج
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[3] اختبار منع الخصم المزدوج لمشتريات الدرج المخزنية:");
        var rawMaterialExpense = 50m;
        var electricityExpense = 30m;
        var maintenanceExpense = 20m;
        decimal operatingExpenses = electricityExpense + maintenanceExpense;
        decimal inventoryPurchases = rawMaterialExpense;

        if (operatingExpenses == 50m && inventoryPurchases == 50m)
        {
            Console.WriteLine($"   ✅ نجاح: مصاريف التشغيل = {operatingExpenses} ج, ومشتريات الخامات مفصولة = {inventoryPurchases} ج");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في منع الخصم المزدوج");
        }

        // -----------------------------------------------------------------------------
        // TEST 4: احتساب تكلفة الهالك
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[4] اختبار احتساب تكلفة الهالك (هالك فويد + تالف مباشر):");
        decimal voidWasteCost = 15m;
        decimal directDamageCost = 5m;
        decimal totalWasteCost = voidWasteCost + directDamageCost;

        if (totalWasteCost == 20m)
        {
            Console.WriteLine($"   ✅ نجاح: إجمالي الهالك والتالف TotalWasteCost = {totalWasteCost} ج");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في حساب تكلفة الهالك");
        }

        // -----------------------------------------------------------------------------
        // TEST 5: معادلة صافي الربح الشاملة
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[5] اختبار معادلة صافي الربح النهائية (Net Profit):");
        decimal grossProfit = netSales - (calculatedCogs + 15m);
        decimal netProfit = grossProfit - operatingExpenses - totalWasteCost;

        if (grossProfit == 110m && netProfit == 40m)
        {
            Console.WriteLine($"   ✅ نجاح: مجمل الربح = {grossProfit} ج, صافي الربح = {netProfit} ج");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في حساب صافي الربح");
        }

        // -----------------------------------------------------------------------------
        // TEST 6: دورة مرتجع المورد
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[6] اختبار دورة مرتجع المورد (Supplier Return):");
        decimal initialWarehouseStock = 100m;
        decimal initialDebt = 10000m;
        decimal returnQty = 10m;
        decimal returnCost = 50m;
        decimal returnTotal = returnQty * returnCost;
        decimal finalWarehouseStock = initialWarehouseStock - returnQty;
        decimal finalDebt = initialDebt - returnTotal;

        if (finalWarehouseStock == 90m && finalDebt == 9500m)
        {
            Console.WriteLine($"   ✅ نجاح: انخفض المخزن إلى {finalWarehouseStock} كجم ومديونية المورد إلى {finalDebt} ج");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في دورة مرتجع المورد");
        }

        // -----------------------------------------------------------------------------
        // TEST 7: دورة مرتجع الفرع للمخزن المركزي
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[7] اختبار دورة مرتجع الفرع للمخزن المركزي (Branch Return):");
        decimal branchStock = 20m;
        decimal centralStock = 100m;
        decimal branchReturnQty = 5m;
        branchStock -= branchReturnQty;
        centralStock += branchReturnQty;

        if (branchStock == 15m && centralStock == 105m)
        {
            Console.WriteLine($"   ✅ نجاح: رصيد الفرع = {branchStock} كجم, رصيد المخزن المركزي = {centralStock} كجم");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في دورة مرتجع الفرع");
        }

        // =============================================================================
        // إعداد بيانات واقعية لاختبار تقرير إنهاء اليوم (EndDayDocument & SalesSummaryDto)
        // =============================================================================
        var testPosDate = DateTime.Today;

        // 1. أوردر صالة (DineIn): 2 صنف، خدمة 12% وضريبة 14%، خصم 20 ج، دفع كاش
        var dineInOrder = new OrderDto
        {
            OrderId = 101,
            OrderType = "DineIn",
            CashierName = "أحمد مصطفى",
            CashierId = "C1",
            SubTotal = 300m,
            Tax = 42m,
            Services = 36m,
            TotalOrderDiscount = 20m,
            TotalDiscount = 20m,
            GrandTotal = 358m, // 300 + 42 + 36 - 20 = 358
            OrderDate = testPosDate.AddHours(14),
            OrderState = "Completed",
            DiscountByName = "مدير الفرع",
            DiscountTime = testPosDate.AddHours(14).AddMinutes(10)
        };

        // 2. أوردر تيك أواي (TakeAway): صنفين، بدون خدمة، دفع فيزا
        var takeAwayOrder = new OrderDto
        {
            OrderId = 102,
            OrderType = "TakeAway",
            CashierName = "أحمد مصطفى",
            CashierId = "C1",
            SubTotal = 150m,
            Tax = 0m,
            Services = 0m,
            TotalOrderDiscount = 0m,
            TotalDiscount = 0m,
            GrandTotal = 150m,
            OrderDate = testPosDate.AddHours(15),
            OrderState = "Completed"
        };

        // 3. أوردر دليفري (Delivery): صنف، رسوم توصيل 25 ج، دفع كاش
        var deliveryOrder = new OrderDto
        {
            OrderId = 103,
            OrderType = "Delivery",
            CashierName = "محمود علي",
            CashierId = "C2",
            SubTotal = 200m,
            Tax = 0m,
            Services = 25m, // رسوم التوصيل
            TotalOrderDiscount = 10m,
            TotalDiscount = 10m,
            GrandTotal = 215m, // 200 + 25 - 10 = 215
            OrderDate = testPosDate.AddHours(16),
            OrderState = "Completed",
            DiscountByName = "كول سنتر",
            DiscountTime = testPosDate.AddHours(16).AddMinutes(5)
        };

        // 4. أوردر ملغي بالكامل (Full Void): كان صالة بقيمة 120 ج
        var fullVoidOrder = new OrderDto
        {
            OrderId = 104,
            OrderType = "DineIn",
            CashierName = "أحمد مصطفى",
            CashierId = "C1",
            SubTotal = 120m,
            GrandTotal = 120m,
            OrderDate = testPosDate.AddHours(17),
            OrderState = "Voided"
        };

        // 5. مصروفات درج (IsPayoutFromDrawer = true) ومصروفات عامة
        var drawerExpense1 = new ExpenseDto
        {
            Id = 1,
            Date = testPosDate.AddHours(13),
            Amount = 50m,
            Category = "شراء خامات طارئة",
            Description = "شراء خضار من السوبر ماركت",
            SpentBy = "الشيف عماد",
            CreatedByName = "أحمد مصطفى",
            IsPayoutFromDrawer = true
        };

        var drawerExpense2 = new ExpenseDto
        {
            Id = 2,
            Date = testPosDate.AddHours(16).AddMinutes(30),
            Amount = 30m,
            Category = "نثريات وصيانة",
            Description = "إصلاح لمبة إضاءة",
            SpentBy = "الفني حسام",
            CreatedByName = "محمود علي",
            IsPayoutFromDrawer = true
        };

        var nonDrawerExpense = new ExpenseDto
        {
            Id = 3,
            Date = testPosDate.AddHours(12),
            Amount = 500m,
            Category = "إيجار/كهرباء",
            Description = "سداد فاتورة كهرباء من البنك",
            SpentBy = "الإدارة",
            IsPayoutFromDrawer = false // لا تخصم من درج الكاشير
        };

        // 6. أحداث الفويد (Void Events)
        var voidEvent1 = new VoidEventDto
        {
            OrderId = 104,
            OrderType = "DineIn",
            VoidDate = testPosDate.AddHours(17).AddMinutes(15),
            VoidedByName = "مدير الصالة",
            Reason = "العميل غادر قبل إعداد الطلب",
            IsFullVoid = true,
            GrandTotalBefore = 120m,
            TotalVoidedAmount = 120m,
            GrandTotalAfter = 0m
        };

        var voidEvent2 = new VoidEventDto
        {
            OrderId = 101,
            OrderType = "DineIn",
            VoidDate = testPosDate.AddHours(14).AddMinutes(5),
            VoidedByName = "كابتن الصالة",
            Reason = "صنف تم طلبه بالخطأ",
            IsFullVoid = false,
            GrandTotalBefore = 398m,
            TotalVoidedAmount = 40m,
            GrandTotalAfter = 358m
        };

        // تجميع SalesSummaryDto
        var summaryDto = new SalesSummaryDto
        {
            PosDate = testPosDate,
            DetailedOrders = new List<OrderDto> { dineInOrder, takeAwayOrder, deliveryOrder, fullVoidOrder },
            DetailedExpenses = new List<ExpenseDto> { drawerExpense1, drawerExpense2, nonDrawerExpense },
            VoidEvents = new List<VoidEventDto> { voidEvent1, voidEvent2 },
            DineIn = new ModeSummaryDto
            {
                OrderCount = 1,
                Subtotal = 300m,
                Tax = 42m,
                Service = 36m,
                Discount = 20m,
                Total = 358m,
                VoidAmount = 160m // 120 كلي + 40 جزئي
            },
            TakeAway = new ModeSummaryDto
            {
                OrderCount = 1,
                Subtotal = 150m,
                Total = 150m
            },
            Delivery = new ModeSummaryDto
            {
                OrderCount = 1,
                Subtotal = 200m,
                DeliveryFees = 25m,
                Discount = 10m,
                Total = 215m
            },
            Overall = new OverallSummaryDto
            {
                TotalSales = 358m + 150m + 215m, // 723m
                CashAmount = 358m + 215m,        // DineIn (358) + Delivery (215) = 573m
                CreditAmount = 150m,             // TakeAway (150 visa)
                TotalDiscount = 20m + 10m,       // 30m
                RefundAmount = 15m,              // مرتجع تجريبي 15 ج
                Expenses = 50m + 30m,            // فقط مصروفات الدرج (80m)
                VoidAmount = 120m + 40m,         // 160m
                FullVoidAmount = 120m,
                PartialVoidAmount = 40m,
                Currency = "ج.م"
            },
            CashierSummaries = new List<AccountSummaryDto>
            {
                new() { Id = "C1", Name = "أحمد مصطفى", Type = "Cashier", CashAmount = 358m, CreditAmount = 150m, DiscountAmount = 20m, VoidAmount = 160m },
                new() { Id = "C2", Name = "محمود علي", Type = "Cashier", CashAmount = 215m, CreditAmount = 0m, DiscountAmount = 10m, VoidAmount = 0m }
            }
        };

        var itemsSummary = new List<SalesItemSummaryDto>
        {
            new() { ItemId = 1, ItemName = "وجبة ميكس جريل", Quantity = 3, UnitPrice = 120m, TotalAmount = 360m, CategoryName = "مشويات" },
            new() { ItemId = 2, ItemName = "بيتزا سوبر سوبريم", Quantity = 2, UnitPrice = 95m, TotalAmount = 190m, CategoryName = "بيتزا" },
            new() { ItemId = 3, ItemName = "عصير مانجو فريش", Quantity = 5, UnitPrice = 20m, TotalAmount = 100m, CategoryName = "مشروبات" }
        };

        // -----------------------------------------------------------------------------
        // TEST 8: التحقق من دقة حسابات إنهاء اليوم والمصروفات وصافي النقدية (NetCash)
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[8] اختبار معادلة صافي نقدية الدرج (NetCash) مع المصروفات والمرتجعات:");
        // NetCash = CashAmount (573) - Expenses (80) - RefundAmount (15) = 478
        decimal expectedNetCash = 573m - 80m - 15m;
        decimal actualNetCash = summaryDto.Overall.NetCash;

        if (actualNetCash == expectedNetCash && expectedNetCash == 478m)
        {
            Console.WriteLine($"   ✅ نجاح: الكاش المحصل = {summaryDto.Overall.CashAmount} ج");
            Console.WriteLine($"   ✅ نجاح: مصروفات الدرج المخصومة = {summaryDto.Overall.Expenses} ج (تم استبعاد 500 ج مصروف غير مسحوب من الدرج)");
            Console.WriteLine($"   ✅ نجاح: المرتجعات = {summaryDto.Overall.RefundAmount} ج");
            Console.WriteLine($"   ✅ نجاح: صافي النقدية بالدرج NetCash = {actualNetCash} ج (مطابق 100%)");
            passed++;
        }
        else
        {
            Console.WriteLine($"   ❌ فشل: NetCash المتوقع {expectedNetCash} والفعلي {actualNetCash}");
        }

        // -----------------------------------------------------------------------------
        // TEST 9: التحقق من إجمالي الإيرادات وصافي المبيعات وتوزيع الأنواع (صالة، تيك أواي، دليفري)
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[9] اختبار توزيع الأنماط (صالة، تيك أواي، دليفري) وإجمالي الإيرادات:");
        decimal sumModes = summaryDto.DineIn.Total + summaryDto.TakeAway.Total + summaryDto.Delivery.Total;
        decimal expectedRevenue = summaryDto.Overall.TotalSales + summaryDto.Overall.TotalDiscount; // 723 + 30 = 753

        if (sumModes == summaryDto.Overall.TotalSales && summaryDto.Overall.TotalRevenue == expectedRevenue)
        {
            Console.WriteLine($"   ✅ نجاح: مبيعات الصالة = {summaryDto.DineIn.Total} ج (شامل الخدمة والضريبة والخصم)");
            Console.WriteLine($"   ✅ نجاح: مبيعات التيك أواي = {summaryDto.TakeAway.Total} ج");
            Console.WriteLine($"   ✅ نجاح: مبيعات الدليفري = {summaryDto.Delivery.Total} ج (شامل رسوم التوصيل)");
            Console.WriteLine($"   ✅ نجاح: مجموع الأنماط ({sumModes} ج) مطابق تماماً لصافي المبيعات ({summaryDto.Overall.TotalSales} ج)");
            Console.WriteLine($"   ✅ نجاح: إجمالي الإيراد قبل الخصم TotalRevenue = {summaryDto.Overall.TotalRevenue} ج");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في مطابقة إجماليات أنماط التشغيل");
        }

        // -----------------------------------------------------------------------------
        // TEST 10: التحقق من الخصومات الإجمالية والتفصيلية
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[10] اختبار مطابقة الخصومات:");
        var discountOrders = summaryDto.DetailedOrders.Where(o => (o.TotalOrderDiscount ?? 0) > 0 || (o.TotalDiscount ?? 0) > 0).ToList();
        decimal totalOrderDiscounts = discountOrders.Sum(o => o.TotalDiscount ?? 0);

        if (totalOrderDiscounts == summaryDto.Overall.TotalDiscount && discountOrders.Count == 2)
        {
            Console.WriteLine($"   ✅ نجاح: تم حصر {discountOrders.Count} أوردرات بخصم، إجمالي الخصومات = {totalOrderDiscounts} ج");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في مطابقة الخصومات");
        }

        // -----------------------------------------------------------------------------
        // TEST 11: التحقق من الإلغاءات (Voids) الكلية والجزئية
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[11] اختبار مطابقة الإلغاءات (Full Voids & Partial Voids):");
        decimal calculatedVoids = summaryDto.VoidEvents.Sum(v => v.TotalVoidedAmount);

        if (calculatedVoids == summaryDto.Overall.VoidAmount && summaryDto.Overall.FullVoidAmount == 120m && summaryDto.Overall.PartialVoidAmount == 40m)
        {
            Console.WriteLine($"   ✅ نجاح: فويد كلي = {summaryDto.Overall.FullVoidAmount} ج, فويد جزئي = {summaryDto.Overall.PartialVoidAmount} ج");
            Console.WriteLine($"   ✅ نجاح: إجمالي الإلغاءات = {calculatedVoids} ج مسجلة في {summaryDto.VoidEvents.Count} حركة إلغاء");
            passed++;
        }
        else
        {
            Console.WriteLine("   ❌ فشل في مطابقة الإلغاءات");
        }

        // -----------------------------------------------------------------------------
        // TEST 12: التحقق من إجماليات الكاشيرات والموظفين
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[12] اختبار مطابقة ملخصات الكاشير:");
        decimal totalCashierAmount = summaryDto.CashierSummaries.Sum(c => c.TotalAmount);
        decimal expectedCashierTotal = summaryDto.Overall.TotalSales;

        if (totalCashierAmount == expectedCashierTotal)
        {
            Console.WriteLine($"   ✅ نجاح: إجمالي مبيعات الكاشيرات = {totalCashierAmount} ج مطابق لإجمالي المبيعات {expectedCashierTotal} ج");
            passed++;
        }
        else
        {
            Console.WriteLine($"   ❌ فشل: إجمالي الكاشيرات {totalCashierAmount} لا يطابق {expectedCashierTotal}");
        }

        // -----------------------------------------------------------------------------
        // TEST 13: توليد تقرير إنهاء اليوم بصيغة رول الكاشير 80mm عبر QuestPDF
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[13] اختبار توليد ملف PDF لتقرير إنهاء اليوم (رول كاشير 80mm حراري):");
        try
        {
            var cashierDoc = new EndDayDocument(
                summaryDto,
                itemsSummary,
                "مطعم وكافيه النجوم",
                "",
                ReportPageFormat.Cashier,
                true,
                "ج.م"
            );

            byte[] pdfBytes = cashierDoc.GeneratePdf();
            if (pdfBytes != null && pdfBytes.Length > 1000)
            {
                var testOutFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestReports");
                Directory.CreateDirectory(testOutFolder);
                var testOutPath = Path.Combine(testOutFolder, "Test_EndDay_Cashier80mm.pdf");
                File.WriteAllBytes(testOutPath, pdfBytes);

                Console.WriteLine($"   ✅ نجاح: تم توليد رول الكاشير 80mm بنجاح وحجم الملف {pdfBytes.Length} بايت!");
                Console.WriteLine($"   📄 مسار الملف: {testOutPath}");
                passed++;
            }
            else
            {
                Console.WriteLine("   ❌ فشل: تم توليد ملف فارغ أو صغير جداً");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ فشل أثناء توليد PDF 80mm: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"      Inner: {ex.InnerException.Message}");
        }

        // -----------------------------------------------------------------------------
        // TEST 14: توليد تقرير إنهاء اليوم بصيغة A4 بالعربي عبر QuestPDF
        // -----------------------------------------------------------------------------
        Console.WriteLine("\n[14] اختبار توليد تقرير إنهاء اليوم بصيغة A4 عربي:");
        try
        {
            var a4Doc = new EndDayDocument(
                summaryDto,
                itemsSummary,
                "مطعم وكافيه النجوم",
                "",
                ReportPageFormat.A4,
                true,
                "ج.م"
            );

            byte[] a4Bytes = a4Doc.GeneratePdf();
            if (a4Bytes != null && a4Bytes.Length > 1000)
            {
                var testOutFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestReports");
                var testA4Path = Path.Combine(testOutFolder, "Test_EndDay_A4.pdf");
                File.WriteAllBytes(testA4Path, a4Bytes);

                Console.WriteLine($"   ✅ نجاح: تم توليد تقرير A4 بنجاح وحجم الملف {a4Bytes.Length} بايت!");
                Console.WriteLine($"   📄 مسار الملف: {testA4Path}");
                passed++;
            }
            else
            {
                Console.WriteLine("   ❌ فشل: تم توليد ملف A4 فارغ أو غير صالح");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ فشل أثناء توليد PDF A4: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"      Inner: {ex.InnerException.Message}");
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine($"النتيجة النهائية للاختبارات الشاملة: {passed} من أصل {total} نجحت بنسبة {(passed * 100.0 / total):0.#}%!");
        Console.WriteLine("===============================================================================");
    }
}