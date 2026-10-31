    private async Task printDeliveryReceipts(OrderDto deliveryOrder, Orders createdOrder, List<string> branchDetails, bool isFollowUp = false)
    {
        var datePart = deliveryOrder.OrderDate!.Value.Date;
        var timeNow = DateTime.Now.TimeOfDay;
        var combined = datePart.Add(timeNow);

        var receipt = new DeliveryReceipt()
        {
            Id = createdOrder.OrderID,
            StoreName = deliveryOrder.BranchName!,
            CashierName = deliveryOrder.CashierName!,
            ReceiptType = OrderTypes.Delivery.ToString(),
            DateCreated = combined,
            PaymentMethod = deliveryOrder.PaymentMethod!.ToString()!,
            FooterMessage = deliveryOrder!.FooterMessage!,
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = deliveryOrder.SubTotal,
            DeliveryFees = deliveryOrder.DeliveryFees ?? 0,
            TotalOrder = deliveryOrder.GrandTotal ?? 0,
            CustomerName = deliveryOrder.CustomerName,
            CustomerFirstPhone = deliveryOrder.Phone1,
            CustomerSecondPhone = deliveryOrder.Phone2,
            Building = deliveryOrder.HomeNum,
            FloorNumber = deliveryOrder.FloorNum,
            FlatNumber = deliveryOrder.ApartmentNum,
            ZoneName = deliveryOrder.ZoneName,
            AddressNote = deliveryOrder.AddressNotice,
            DeliveryName = deliveryOrder.DriverName,
            CustomerAddress = deliveryOrder.StreetName,
            IsFollowUp = isFollowUp
        };

        var deliveryItems = deliveryOrder.OrderDetails!;

        var outputPath = await CreateDeliveryReceiptLayOut(receipt, deliveryItems);

        await PrintCashReceipt(deliveryOrder, outputPath);
    }

    private async Task<string> CreateDeliveryReceiptLayOut(DeliveryReceipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new DeliveryReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-delivery-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }
