/* CafeFlow örnek sipariş verisi
   - Uygulamanın kendi başlangıç verisinden (SeedData: kategori, ürün, seçenek, Masa 7 ve 12) SONRA çalıştırılır.
   - Ürün, seçenek ve masa Id'leri ada / masa numarasına göre bulunur; Id sırasına bağlı değildir.
   - Tarihler çalıştırıldığı güne göredir (son 7 gün); dashboard ve filtreler dolu görünür.
   - Fiyatlar uygulama kurallarıyla aynıdır: taban fiyat + seçenek farkları (Decorator),
     teslimat 30 TL, 600 TL ve üstü ücretsiz; gel-al ve masa ücretsiz (Strategy).
   - CF-DEMO siparişleri zaten varsa hiçbir şey yapmaz (tekrar çalıştırmak güvenlidir).
   Kullanım: sqlcmd -S .\SQLEXPRESS -d CafeOrderDb -E -I -f 65001 -i database\CafeOrderDb_sample_orders.sql */
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON;
IF EXISTS (SELECT 1 FROM Orders WHERE OrderNo LIKE 'CF-DEMO%') BEGIN PRINT N'Örnek siparişler zaten var.'; RETURN; END
IF NOT EXISTS (SELECT 1 FROM Products) BEGIN PRINT N'Önce uygulamayı bir kez çalıştırın (başlangıç verisi yok).'; RETURN; END
BEGIN TRAN;
DECLARE @today datetime2 = CAST(CAST(GETDATE() AS date) AS datetime2);
DECLARE @now datetime2 = DATEADD(MINUTE, -90, GETDATE());
DECLARE @c int, @o int, @d int, @t datetime2;

-- CF-DEMO0001: Delivery, Delivered, ara toplam 715, ücret 0, toplam 715
SET @t = DATEADD(HOUR, 9, DATEADD(DAY, -6, @today));
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05321112233';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Ayşe Yılmaz', '05321112233'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0001', 'Delivery', 'Delivered', @c, NULL, N'Moda Cad. No:12 Kadıköy', NULL, NULL, N'Zil bozuk, lütfen arayın', 715, 0, 715, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 225, 2, 450 FROM Products WHERE Name = N'Vanilyalı Latte';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Büyük';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Yulaf Sütü';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'ExtraShot' AND Value = 'Evet';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 170, 1, 170 FROM Products WHERE Name = N'Vanilyalı Latte';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Küçük';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Normal Süt';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Syrup' AND Value = 'Vanilya Şurubu';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 95, 1, 95 FROM Products WHERE Name = N'Tereyağlı Kruvasan';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Ready', DATEADD(MINUTE, 14, @t), NULL),
(@o, 'Ready', 'Delivered', DATEADD(MINUTE, 21, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0001: hazırlanacak 4 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0001 siparişi verildi. Toplam: ₺715,00.', 1, @t),
(@o, 'Admin', N'Yeni Teslimat siparişi: CF-DEMO0001.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0001: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0001: Hazırlanıyor → Hazır.', 1, DATEADD(MINUTE, 14, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0001: Hazır → Teslim Edildi.', 1, DATEADD(MINUTE, 21, @t)),
(@o, 'Cashier', N'CF-DEMO0001 siparişi teslim edildi.', 1, DATEADD(MINUTE, 21, @t));

-- CF-DEMO0002: Pickup, Delivered, ara toplam 705, ücret 0, toplam 705
SET @t = DATEADD(HOUR, 10, DATEADD(DAY, -5, @today));
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05334445566';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Mehmet Demir', '05334445566'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0002', 'Pickup', 'Delivered', @c, NULL, NULL, NULL, DATEADD(HOUR, 1, @t), NULL, 705, 0, 705, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 245, 2, 490 FROM Products WHERE Name = N'Lotus Cheesecake';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 215, 1, 215 FROM Products WHERE Name = N'Buzlu İspanyol Latte';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Orta';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Badem Sütü';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Syrup' AND Value = 'Karamel Şurubu';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Ready', DATEADD(MINUTE, 14, @t), NULL),
(@o, 'Ready', 'Delivered', DATEADD(MINUTE, 21, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0002: hazırlanacak 3 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0002 siparişi verildi. Toplam: ₺705,00.', 1, @t),
(@o, 'Admin', N'Yeni Gel-Al siparişi: CF-DEMO0002.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0002: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0002: Hazırlanıyor → Hazır.', 1, DATEADD(MINUTE, 14, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0002: Hazır → Teslim Edildi.', 1, DATEADD(MINUTE, 21, @t)),
(@o, 'Cashier', N'CF-DEMO0002 siparişi teslim edildi.', 1, DATEADD(MINUTE, 21, @t));

-- CF-DEMO0003: Table, Delivered, ara toplam 885, ücret 0, toplam 885
SET @t = DATEADD(HOUR, 14, DATEADD(DAY, -5, @today));
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0003', 'Table', 'Delivered', NULL, (SELECT Id FROM Tables WHERE Number = 7), NULL, NULL, NULL, N'Pencere kenarı', 885, 0, 885, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 220, 3, 660 FROM Products WHERE Name = N'Çilekli Bulut Latte';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Orta';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Normal Süt';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 225, 1, 225 FROM Products WHERE Name = N'Pembe Kadife Pasta';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Ready', DATEADD(MINUTE, 14, @t), NULL),
(@o, 'Ready', 'Delivered', DATEADD(MINUTE, 21, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0003: hazırlanacak 4 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0003 siparişi verildi. Toplam: ₺885,00.', 1, @t),
(@o, 'Admin', N'Yeni Masa siparişi: CF-DEMO0003.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0003: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0003: Hazırlanıyor → Hazır.', 1, DATEADD(MINUTE, 14, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0003: Hazır → Teslim Edildi.', 1, DATEADD(MINUTE, 21, @t)),
(@o, 'Cashier', N'CF-DEMO0003 siparişi teslim edildi.', 1, DATEADD(MINUTE, 21, @t));

-- CF-DEMO0004: Delivery, Cancelled, ara toplam 410, ücret 30, toplam 440
SET @t = DATEADD(HOUR, 11, DATEADD(DAY, -4, @today));
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05447778899';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Zeynep Kaya', '05447778899'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0004', 'Delivery', 'Cancelled', @c, NULL, N'Bağdat Cad. 200 Maltepe', NULL, NULL, NULL, 410, 30, 440, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 255, 1, 255 FROM Products WHERE Name = N'Hindi ve Brie Panini';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 155, 1, 155 FROM Products WHERE Name = N'Cold Brew';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Cancelled', DATEADD(MINUTE, 7, @t), N'Müşteri iptal etti');
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0004: hazırlanacak 2 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0004 siparişi verildi. Toplam: ₺440,00.', 1, @t),
(@o, 'Admin', N'Yeni Teslimat siparişi: CF-DEMO0004.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0004: Alındı → İptal Edildi.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Kitchen', N'CF-DEMO0004 siparişi iptal edildi. Hazırlamayı durdurun.', 1, DATEADD(MINUTE, 7, @t));

-- CF-DEMO0005: Table, Delivered, ara toplam 710, ücret 0, toplam 710
SET @t = DATEADD(HOUR, 16, DATEADD(DAY, -3, @today));
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0005', 'Table', 'Delivered', NULL, (SELECT Id FROM Tables WHERE Number = 12), NULL, NULL, NULL, NULL, 710, 0, 710, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 265, 2, 530 FROM Products WHERE Name = N'Güllü Matcha';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Büyük';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Yulaf Sütü';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Syrup' AND Value = 'Vanilya Şurubu';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 180, 1, 180 FROM Products WHERE Name = N'Ahududulu Makaron';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Ready', DATEADD(MINUTE, 14, @t), NULL),
(@o, 'Ready', 'Delivered', DATEADD(MINUTE, 21, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0005: hazırlanacak 3 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0005 siparişi verildi. Toplam: ₺710,00.', 1, @t),
(@o, 'Admin', N'Yeni Masa siparişi: CF-DEMO0005.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0005: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0005: Hazırlanıyor → Hazır.', 1, DATEADD(MINUTE, 14, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0005: Hazır → Teslim Edildi.', 1, DATEADD(MINUTE, 21, @t)),
(@o, 'Cashier', N'CF-DEMO0005 siparişi teslim edildi.', 1, DATEADD(MINUTE, 21, @t));

-- CF-DEMO0006: Pickup, Delivered, ara toplam 375, ücret 0, toplam 375
SET @t = DATEADD(HOUR, 8, DATEADD(DAY, -2, @today));
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05551234567';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Ali Vural', '05551234567'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0006', 'Pickup', 'Delivered', @c, NULL, NULL, NULL, DATEADD(HOUR, 1, @t), NULL, 375, 0, 375, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 145, 1, 145 FROM Products WHERE Name = N'Klasik Cappuccino';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Küçük';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Normal Süt';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 115, 2, 230 FROM Products WHERE Name = N'Tarçınlı Rulo';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Ready', DATEADD(MINUTE, 14, @t), NULL),
(@o, 'Ready', 'Delivered', DATEADD(MINUTE, 21, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0006: hazırlanacak 3 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0006 siparişi verildi. Toplam: ₺375,00.', 1, @t),
(@o, 'Admin', N'Yeni Gel-Al siparişi: CF-DEMO0006.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0006: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0006: Hazırlanıyor → Hazır.', 1, DATEADD(MINUTE, 14, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0006: Hazır → Teslim Edildi.', 1, DATEADD(MINUTE, 21, @t)),
(@o, 'Cashier', N'CF-DEMO0006 siparişi teslim edildi.', 1, DATEADD(MINUTE, 21, @t));

-- CF-DEMO0007: Pickup, Cancelled, ara toplam 230, ücret 0, toplam 230
SET @t = DATEADD(HOUR, 12, DATEADD(DAY, -1, @today));
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05447778899';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Zeynep Kaya', '05447778899'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0007', 'Pickup', 'Cancelled', @c, NULL, NULL, NULL, DATEADD(HOUR, 2, @t), NULL, 230, 0, 230, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 230, 1, 230 FROM Products WHERE Name = N'Çilekli Pasta';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Cancelled', DATEADD(MINUTE, 14, @t), N'Müşteri iptal etti');
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0007: hazırlanacak 1 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0007 siparişi verildi. Toplam: ₺230,00.', 1, @t),
(@o, 'Admin', N'Yeni Gel-Al siparişi: CF-DEMO0007.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0007: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0007: Hazırlanıyor → İptal Edildi.', 1, DATEADD(MINUTE, 14, @t)),
(@o, 'Kitchen', N'CF-DEMO0007 siparişi iptal edildi. Hazırlamayı durdurun.', 1, DATEADD(MINUTE, 14, @t));

-- CF-DEMO0008: Delivery, Ready, ara toplam 700, ücret 0, toplam 700
SET @t = DATEADD(HOUR, 13, DATEADD(DAY, -1, @today));
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05321112233';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Ayşe Yılmaz', '05321112233'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0008', 'Delivery', 'Ready', @c, NULL, N'Moda Cad. No:12 Kadıköy', NULL, NULL, NULL, 700, 0, 700, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 235, 2, 470 FROM Products WHERE Name = N'Avokadolu Tost';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 230, 1, 230 FROM Products WHERE Name = N'Buzlu Mocha';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Büyük';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Normal Süt';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'ExtraShot' AND Value = 'Evet';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL),
(@o, 'Preparing', 'Ready', DATEADD(MINUTE, 14, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0008: hazırlanacak 3 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0008 siparişi verildi. Toplam: ₺700,00.', 1, @t),
(@o, 'Admin', N'Yeni Teslimat siparişi: CF-DEMO0008.', 1, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0008: Alındı → Hazırlanıyor.', 1, DATEADD(MINUTE, 7, @t)),
(@o, 'Admin', N'Sipariş CF-DEMO0008: Hazırlanıyor → Hazır.', 1, DATEADD(MINUTE, 14, @t));

-- CF-DEMO0009: Table, Preparing, ara toplam 525, ücret 0, toplam 525
SET @t = @now;
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0009', 'Table', 'Preparing', NULL, (SELECT Id FROM Tables WHERE Number = 7), NULL, NULL, NULL, NULL, 525, 0, 525, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 200, 2, 400 FROM Products WHERE Name = N'Karamel Macchiato';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Orta';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Yulaf Sütü';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 125, 1, 125 FROM Products WHERE Name = N'Bademli Kruvasan';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL),
(@o, 'Received', 'Preparing', DATEADD(MINUTE, 7, @t), NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0009: hazırlanacak 3 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0009 siparişi verildi. Toplam: ₺525,00.', 1, @t),
(@o, 'Admin', N'Yeni Masa siparişi: CF-DEMO0009.', 0, @t),
(@o, 'Admin', N'Sipariş CF-DEMO0009: Alındı → Hazırlanıyor.', 0, DATEADD(MINUTE, 7, @t));

-- CF-DEMO0010: Pickup, Received, ara toplam 215, ücret 0, toplam 215
SET @t = @now;
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05334445566';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Mehmet Demir', '05334445566'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0010', 'Pickup', 'Received', @c, NULL, NULL, NULL, DATEADD(HOUR, 1, @t), N'Şekersiz olsun', 215, 0, 215, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 215, 1, 215 FROM Products WHERE Name = N'Pembe Orman Meyveli Frappe';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0010: hazırlanacak 1 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0010 siparişi verildi. Toplam: ₺215,00.', 1, @t),
(@o, 'Admin', N'Yeni Gel-Al siparişi: CF-DEMO0010.', 0, @t);

-- CF-DEMO0011: Delivery, Received, ara toplam 665, ücret 0, toplam 665
SET @t = @now;
SET @c = NULL;
SELECT @c = Id FROM Customers WHERE Phone = '05061239876';
IF @c IS NULL BEGIN INSERT INTO Customers (FullName, Phone) VALUES (N'Can Öztürk', '05061239876'); SET @c = SCOPE_IDENTITY(); END
INSERT INTO Orders (OrderNo, Channel, Status, CustomerId, TableId, Address, AddressNotes, PickupTime, Note, Subtotal, DeliveryFee, Total, CreatedAt)
VALUES ('CF-DEMO0011', 'Delivery', 'Received', @c, NULL, N'Cihangir Mah. Sıraselviler Cad. 45 Beyoğlu', NULL, NULL, NULL, 665, 0, 665, @t);
SET @o = SCOPE_IDENTITY();
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 245, 2, 490 FROM Products WHERE Name = N'Tavuklu Sezar Dürüm';
INSERT INTO OrderDetails (OrderId, ProductId, ProductName, BasePrice, UnitPrice, Quantity, LineTotal) SELECT @o, Id, Name, BasePrice, 175, 1, 175 FROM Products WHERE Name = N'Vanilyalı Latte';
SET @d = SCOPE_IDENTITY();
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Size' AND Value = 'Orta';
INSERT INTO OrderDetailOptions (OrderDetailId, ProductOptionId, GroupKey, Value, PriceDelta) SELECT @d, Id, GroupKey, Value, PriceDelta FROM ProductOptions WHERE GroupKey = 'Milk' AND Value = 'Normal Süt';
INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, ChangedAt, Note) VALUES
(@o, NULL, 'Received', @t, NULL);
INSERT INTO Notifications (OrderId, Target, Message, IsRead, CreatedAt) VALUES
(@o, 'Kitchen', N'Yeni sipariş CF-DEMO0011: hazırlanacak 3 ürün.', 1, @t),
(@o, 'Cashier', N'CF-DEMO0011 siparişi verildi. Toplam: ₺665,00.', 1, @t),
(@o, 'Admin', N'Yeni Teslimat siparişi: CF-DEMO0011.', 0, @t);

COMMIT;
PRINT N'Örnek siparişler eklendi: 11 sipariş (teslimat, gel-al, masa; bütün durumlar).';
