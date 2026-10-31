using System;
using System.Collections.Generic;
using System.Linq;
using BlazorBase.Models;
using POS.Contract.Models;
using POS.Contract.Dtos.DineIn;

namespace BlazorBase.Helpers;

public static class DineInOrderMapper
{
    public static DineInOrderDto MapToDineInOrderDto(DineInOrderDetails orderDetails, int orderId, int branchId, string? branchName)
    {
        var items = orderDetails.BasicOrderDetails?.Items ?? new();
        decimal lineDiscountSum = items.Sum(i => i.TotalDiscountPrice ?? 0M);
        decimal orderDiscountValue = orderDetails.BasicOrderDetails?.OrderDiscount?.Value ?? 0M;

        var dineInOrder = new DineInOrderDto
        {
            OrderId = orderId,
            BranchId = branchId,
            BranchName = branchName,
            CashierId = orderDetails.BasicOrderDetails?.CashierName,
            CashierName = orderDetails.BasicOrderDetails?.CashierName,
            CaptainId = orderDetails.CaptainId,
            CaptainName = orderDetails.CaptainName,
            TableId = orderDetails.RelatedTableId ?? 0,
            TableName = orderDetails.RelatedTableName,
            OrderDateTime = orderDetails.BasicOrderDetails?.OrderDataTime ?? DateTime.Now,
            OrderState = "Open",
            Subtotal = orderDetails.BasicOrderDetails?.Account, // This is Gross from _financeSettingsList[0]
            Tax = orderDetails.BasicOrderDetails?.Tax,
            Service = orderDetails.BasicOrderDetails?.Service,
            DiscountAmount = orderDetails.BasicOrderDetails?.OrderDiscount?.Value,
            DiscountPercentage = orderDetails.BasicOrderDetails?.OrderDiscount?.Percentage,
            DiscountType = orderDetails.BasicOrderDetails?.OrderDiscount?.DiscountType,
            DiscountReason = orderDetails.BasicOrderDetails?.OrderDiscount?.DiscountReason,
            DiscountedItems = lineDiscountSum,
            TotalDiscount = orderDiscountValue + lineDiscountSum,
            GrandTotal = orderDetails.BasicOrderDetails?.Total,
            PaymentMethod = orderDetails.BasicOrderDetails?.PaymentMethod ?? global::POS.Contract.Models.PaymentMethod.Cash,
            OrderNotice = orderDetails.BasicOrderDetails?.OrderNote,
            CustomerName = orderDetails.BasicOrderDetails?.CustomerName,
            CustomerPhone = orderDetails.BasicOrderDetails?.CustomerPhone,
            MachineName = orderDetails.BasicOrderDetails?.MachineName,
            CaptainTipsDeduction = orderDetails.BasicOrderDetails?.CaptainTipsDeduction,
            OrderDetails = MapToOrderItemsDetailsDto(orderDetails.BasicOrderDetails?.Items)
        };

        return dineInOrder;
    }

    public static List<OrderItemsDetailsDto> MapToOrderItemsDetailsDto(List<TableItem>? items)
    {
        if (items == null || !items.Any())
            return new List<OrderItemsDetailsDto>();

        var orderItems = new List<OrderItemsDetailsDto>();

        foreach (var item in items)
        {
            var orderItem = new OrderItemsDetailsDto
            {
                Id = item.DatabaseId,
                MenuSalesItemId = item.Id,
                ItemName = item.Name,
                ItemNameAr = item.NameAr,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                Price = item.Price,
                Quantity = item.Quantity,
                TotalAmount = item.TotalAmount, 
                TotalAfterDiscount = item.TotalAfterDiscount, 
                Total = item.Total,
                Discount = item.HasDiscount,
                TotalDiscountPrice = item.TotalDiscountPrice,
                TotalDiscountPercentage = item.DiscountPercentage,
                TotalDiscountAmount = item.DiscountAmount,
                OrderType = "DineIn",
                OrderItemAttributes = MapToOrderItemAttributesDto(item.Attributes),
                // Map printing properties
                ItemKitchenTypeId = item.ItemKitchenTypeId,
                CategoryKitchenTypeId = item.CategoryKitchenTypeId,
                PrintInBackupReceiptFromItem = item.PrintInBackupReceiptFromItem,
                PrintInBackupReceiptFromCategory = item.PrintInBackupReceiptFromCategory
            };

            if (!string.IsNullOrEmpty(item.LineComment))
            {
                orderItem.OrderItemComments = new List<OrderItemCommentDto>
                {
                    new OrderItemCommentDto { Comment = item.LineComment, CommentTime = DateTime.Now }
                };
            }

            orderItems.Add(orderItem);
        }

        return orderItems;
    }

    private static List<OrderItemAttributesDto> MapToOrderItemAttributesDto(List<AttributeDto>? attributes)
    {
        if (attributes == null || !attributes.Any())
            return new List<OrderItemAttributesDto>();

        return attributes.Select(attr => new OrderItemAttributesDto
        {
            AttributeItemId = attr.Id,
            AttributeName = attr.Name
        }).ToList();
    }

    public static DineInOrderDetails MapToDineInOrderDetails(DineInOrderDto dineInOrder)
    {
        return new DineInOrderDetails
        {
            DatabaseId = dineInOrder.Id,
            CaptainId = dineInOrder.CaptainId,
            CaptainName = dineInOrder.CaptainName,
            RelatedTableId = dineInOrder.TableId,
            RelatedTableName = dineInOrder.TableName,
            PrintCount = dineInOrder?.PrintCount ?? 0,
            BasicOrderDetails = new BlazorBase.Models.OrderDetails
            {
                OrderId = dineInOrder!.OrderId,
                CashierName = dineInOrder.CashierName,
                OrderType = "DineIn",
                OrderDataTime = dineInOrder.OrderDateTime,
                Items = MapToTableItems(dineInOrder.OrderDetails),
                Account = dineInOrder.Subtotal ?? 0,
                Total = dineInOrder.GrandTotal ?? 0,
                Service = dineInOrder.Service ?? 0,
                Tax = dineInOrder.Tax ?? 0,
                OrderDiscount = new OrderDiscount
                {
                    Value = dineInOrder.DiscountAmount ?? 0,
                    Percentage = dineInOrder.DiscountPercentage ?? 0,
                    DiscountType = dineInOrder.DiscountType,
                    DiscountReason = dineInOrder.DiscountReason
                },
                CustomerName = dineInOrder.CustomerName,
                CustomerPhone = dineInOrder.CustomerPhone,
                PaymentMethod = dineInOrder.PaymentMethod ?? global::POS.Contract.Models.PaymentMethod.Cash,
                OrderNote = dineInOrder.OrderNotice,
                MachineName = dineInOrder.MachineName,
                CaptainTipsDeduction = dineInOrder.CaptainTipsDeduction
            }
        };
    }

    private static List<TableItem> MapToTableItems(List<OrderItemsDetailsDto>? orderDetails)
    {
        if (orderDetails == null || !orderDetails.Any())
            return new List<TableItem>();

        return orderDetails.Select(item => new TableItem
        {
            DatabaseId = item.Id,
            Id = item.MenuSalesItemId ?? 0,
            Name = item.ItemName ?? "",
            NameAr = item.ItemNameAr,
            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,
            Price = item.Price ?? 0,
            Quantity = item.Quantity ?? 0,
            Total = item.Total, 
            TotalAmount = item.TotalAmount, 
            TotalAfterDiscount = item.TotalAfterDiscount,
            HasDiscount = item.Discount ?? false,
            DiscountPercentage = item.TotalDiscountPercentage,
            DiscountAmount = item.TotalDiscountAmount,
            TotalDiscountPrice = item.TotalDiscountPrice,
            Attributes = MapToAttributeDtos(item.OrderItemAttributes),
            LineComment = item.OrderItemComments?.FirstOrDefault()?.Comment,
            IsReadOnly = true,
            // Map printing properties back
            ItemKitchenTypeId = item.ItemKitchenTypeId,
            CategoryKitchenTypeId = item.CategoryKitchenTypeId,
            PrintInBackupReceiptFromItem = item.PrintInBackupReceiptFromItem,
            PrintInBackupReceiptFromCategory = item.PrintInBackupReceiptFromCategory,
            IsVoided = item.IsVoided ?? false,
            TotalVoidAmount = item.TotalVoidAmount,
            VoidBy = item.VoidBy,
            VoidByName = item.VoidByName,
            VoidTime = item.VoidTime,
            VoidReason = item.VoidReason
        }).ToList();
    }

    private static List<AttributeDto> MapToAttributeDtos(List<OrderItemAttributesDto>? orderItemAttributes)
    {
        if (orderItemAttributes == null || !orderItemAttributes.Any())
            return new List<AttributeDto>();

        return orderItemAttributes.Select(attr => new AttributeDto
        {
            Id = attr.AttributeItemId ?? 0,
            Name = attr.AttributeName ?? ""
        }).ToList();
    }
}
