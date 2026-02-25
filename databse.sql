-- MySQL dump 10.13  Distrib 8.0.42, for Win64 (x86_64)
--
-- Host: localhost    Database: hardwarenew
-- ------------------------------------------------------
-- Server version	8.0.42

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `bill_items`
--

DROP TABLE IF EXISTS `bill_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bill_items` (
  `bill_item_id` int NOT NULL AUTO_INCREMENT,
  `bill_id` int NOT NULL,
  `product_id` int NOT NULL,
  `variant_id` int NOT NULL,
  `quantity` decimal(10,2) NOT NULL,
  `unit_of_measure` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `unit_price` decimal(10,2) NOT NULL,
  `line_total` decimal(12,2) NOT NULL,
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`bill_item_id`),
  KEY `idx_bill` (`bill_id`),
  KEY `idx_product` (`product_id`),
  KEY `idx_variant` (`variant_id`),
  CONSTRAINT `bill_items_ibfk_1` FOREIGN KEY (`bill_id`) REFERENCES `bills` (`bill_id`) ON DELETE CASCADE,
  CONSTRAINT `bill_items_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`),
  CONSTRAINT `bill_items_ibfk_3` FOREIGN KEY (`variant_id`) REFERENCES `product_variants` (`variant_id`),
  CONSTRAINT `chk_bill_item_price` CHECK ((`unit_price` >= 0)),
  CONSTRAINT `chk_bill_item_qty` CHECK ((`quantity` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bill line items - Stock REDUCED via trigger';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bill_items`
--

LOCK TABLES `bill_items` WRITE;
/*!40000 ALTER TABLE `bill_items` DISABLE KEYS */;
INSERT INTO `bill_items` VALUES (1,2,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(2,3,1,1,1.00,'FT',145.30,145.30,NULL),(3,4,1,1,1.00,'FT',145.30,145.30,NULL),(4,5,1,1,1.00,'FT',145.30,145.30,NULL),(5,6,1,1,1.00,'FT',145.30,145.30,NULL),(6,11,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(7,11,1,1,3.00,'FT',145.30,435.90,NULL),(8,12,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(9,12,1,1,3.00,'FT',145.30,435.90,NULL),(10,13,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(11,13,1,1,3.00,'FT',145.30,435.90,NULL),(12,14,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(13,14,1,1,3.00,'FT',145.30,435.90,NULL),(14,15,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(15,15,1,1,3.00,'FT',145.30,435.90,NULL),(17,17,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(23,28,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(24,29,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(25,30,1,2,1.00,'FT',188.10,188.10,NULL),(26,31,1,2,5.00,'FT',188.10,940.50,NULL),(27,31,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(28,32,4,6,4.00,'PCS',120.00,480.00,NULL),(29,32,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(30,33,1,2,5.00,'FT',188.10,940.50,NULL),(31,33,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(32,34,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(33,34,1,1,3.00,'FT',145.30,435.90,NULL);
/*!40000 ALTER TABLE `bill_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bills`
--

DROP TABLE IF EXISTS `bills`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bills` (
  `bill_id` int NOT NULL AUTO_INCREMENT,
  `bill_number` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `customer_id` int DEFAULT NULL,
  `staff_id` int NOT NULL,
  `bill_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `subtotal` decimal(12,2) NOT NULL DEFAULT '0.00',
  `discount_percentage` decimal(5,2) DEFAULT '0.00',
  `discount_amount` decimal(12,2) DEFAULT '0.00',
  `tax_percentage` decimal(5,2) DEFAULT '0.00',
  `tax_amount` decimal(12,2) DEFAULT '0.00',
  `total_amount` decimal(12,2) NOT NULL DEFAULT '0.00',
  `amount_paid` decimal(12,2) DEFAULT '0.00',
  `amount_due` decimal(12,2) DEFAULT '0.00' COMMENT 'Remaining balance (for credit customers)',
  `payment_status_id` int NOT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`bill_id`),
  UNIQUE KEY `bill_number_UNIQUE` (`bill_number`),
  KEY `staff_id` (`staff_id`),
  KEY `idx_customer` (`customer_id`),
  KEY `idx_payment_status` (`payment_status_id`),
  KEY `idx_date` (`bill_date`),
  KEY `idx_bill_date_status` (`bill_date`,`payment_status_id`),
  CONSTRAINT `bills_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  CONSTRAINT `bills_ibfk_3` FOREIGN KEY (`staff_id`) REFERENCES `staff` (`staff_id`),
  CONSTRAINT `bills_ibfk_4` FOREIGN KEY (`payment_status_id`) REFERENCES `lookup` (`lookup_id`),
  CONSTRAINT `chk_bill_amounts` CHECK (((`subtotal` >= 0) and (`total_amount` >= 0)))
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bills/Invoices - REDUCES inventory when items added';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bills`
--

LOCK TABLES `bills` WRITE;
/*!40000 ALTER TABLE `bills` DISABLE KEYS */;
INSERT INTO `bills` VALUES (1,'',2,2,'2026-02-14 11:37:14',10000.00,0.00,0.00,0.00,0.00,10000.00,10100.00,4900.00,11,'2026-02-14 11:37:14','2026-02-21 17:04:37'),(2,'INV-2026-0216005514',1,1,'2026-02-15 19:54:24',150.00,0.00,0.00,0.00,0.00,150.00,150.00,0.00,12,'2026-02-15 19:55:14','2026-02-15 19:55:14'),(3,'INV-2026-0216104048',1,1,'2026-02-16 05:40:30',145.30,0.00,0.00,0.00,0.00,145.30,145.30,0.00,12,'2026-02-16 05:40:48','2026-02-16 05:40:48'),(4,'INV-2026-0216104230',1,1,'2026-02-16 05:42:15',145.30,0.00,0.00,0.00,0.00,145.30,145.30,0.00,12,'2026-02-16 05:42:30','2026-02-16 05:42:30'),(5,'INV-2026-0216104418',1,1,'2026-02-16 05:42:15',145.30,0.00,0.00,0.00,0.00,145.30,145.30,0.00,12,'2026-02-16 05:44:18','2026-02-16 05:44:18'),(6,'INV-2026-0216104737',1,1,'2026-02-16 05:47:14',145.30,0.00,0.00,0.00,0.00,145.30,145.30,0.00,12,'2026-02-16 05:47:37','2026-02-16 05:47:37'),(11,'INV-2026-0216233545',1,1,'2026-02-16 18:35:33',700.90,0.00,0.00,0.00,0.00,700.90,700.90,0.00,12,'2026-02-16 18:35:45','2026-02-16 18:35:45'),(12,'INV-2026-0216234137',1,1,'2026-02-16 18:41:20',700.90,0.00,0.00,0.00,0.00,700.90,700.90,0.00,12,'2026-02-16 18:41:37','2026-02-16 18:41:37'),(13,'INV-2026-0216234226',1,1,'2026-02-16 18:42:15',700.90,0.00,0.00,0.00,0.00,700.90,700.90,0.00,12,'2026-02-16 18:42:26','2026-02-16 18:42:26'),(14,'INV-2026-0216234450',1,1,'2026-02-16 18:44:38',700.90,0.00,0.00,0.00,0.00,700.90,700.90,0.00,12,'2026-02-16 18:44:50','2026-02-16 18:44:50'),(15,'INV-2026-0216234650',1,1,'2026-02-16 18:46:38',700.90,0.00,0.00,0.00,0.00,700.90,700.90,0.00,14,'2026-02-16 18:46:50','2026-02-21 06:17:26'),(17,'INV-2026-0216235355',1,1,'2026-02-16 18:51:21',300.00,0.00,0.00,0.00,0.00,300.00,300.00,0.00,12,'2026-02-16 18:53:55','2026-02-16 18:53:55'),(28,'INV-2026-0217180140',1,1,'2026-02-17 13:01:31',150.00,0.00,0.00,0.00,0.00,150.00,150.00,0.00,12,'2026-02-17 13:01:40','2026-02-17 13:01:40'),(29,'INV-2026-0217180236',1,1,'2026-02-17 13:01:31',150.00,0.00,0.00,0.00,0.00,150.00,150.00,0.00,12,'2026-02-17 13:02:36','2026-02-17 13:02:36'),(30,'INV-2026-0218105819',1,1,'2026-02-18 05:57:59',188.10,0.00,0.00,0.00,0.00,188.10,188.10,0.00,12,'2026-02-18 05:58:19','2026-02-18 05:58:19'),(31,'INV-2026-0220225054',2,1,'2026-02-20 09:30:00',1500.00,0.00,50.00,0.00,0.00,1500.00,1000.00,500.00,11,'2026-02-20 17:50:54','2026-02-20 17:50:54'),(32,'INV-2026-0221112853',1,1,'2026-02-21 06:28:30',630.00,0.00,0.00,0.00,0.00,630.00,630.00,0.00,12,'2026-02-21 06:28:53','2026-02-21 06:28:53'),(33,'INV-2026-0221113040',2,1,'2026-02-20 09:30:00',1500.00,0.00,50.00,0.00,0.00,1500.00,1000.00,500.00,11,'2026-02-21 06:30:40','2026-02-21 06:30:40'),(34,'INV-2026-0223215203',2,1,'2026-02-23 16:43:13',715.90,0.00,0.00,0.00,0.00,715.90,600.00,115.90,11,'2026-02-23 16:52:03','2026-02-23 16:52:03');
/*!40000 ALTER TABLE `bills` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `customerpricerecord`
--

DROP TABLE IF EXISTS `customerpricerecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customerpricerecord` (
  `record_id` int NOT NULL AUTO_INCREMENT,
  `customer_id` int NOT NULL,
  `date` date NOT NULL,
  `payment` decimal(10,2) NOT NULL,
  `remarks` varchar(255) DEFAULT NULL,
  `bill_id` int NOT NULL,
  PRIMARY KEY (`record_id`),
  KEY `customer_id` (`customer_id`),
  KEY `bill_id` (`bill_id`),
  CONSTRAINT `customerpricerecord_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  CONSTRAINT `customerpricerecord_ibfk_2` FOREIGN KEY (`bill_id`) REFERENCES `bills` (`bill_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=153 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customerpricerecord`
--

LOCK TABLES `customerpricerecord` WRITE;
/*!40000 ALTER TABLE `customerpricerecord` DISABLE KEYS */;
INSERT INTO `customerpricerecord` VALUES (148,2,'2026-02-17',5000.00,'Payment applied to bill #1',1),(149,2,'2026-02-20',1000.00,'Bill: INV-2026-0220225054 | Total: Rs. 1,500.00 | Paid: Rs. 1,000.00 | Due: Rs. 500.00',31),(150,2,'2026-02-20',1000.00,'Bill: INV-2026-0221113040 | Total: Rs. 1,500.00 | Paid: Rs. 1,000.00 | Due: Rs. 500.00',33),(151,2,'2026-02-21',100.00,'Payment via bank transfer',1),(152,2,'2026-02-23',600.00,'Bill: INV-2026-0223215203 | Total: Rs. 715.90 | Paid: Rs. 600.00 | Due: Rs. 115.90',34);
/*!40000 ALTER TABLE `customerpricerecord` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `customers`
--

DROP TABLE IF EXISTS `customers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customers` (
  `customer_id` int NOT NULL AUTO_INCREMENT,
  `full_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `phone` varchar(15) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `current_balance` decimal(12,2) DEFAULT '0.00' COMMENT 'Outstanding amount owed',
  `customer_type` enum('regular','wholesale','retail','contractor','walkin') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT 'retail',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`customer_id`),
  KEY `idx_contact` (`phone`),
  KEY `idx_active` (`is_active`),
  KEY `idx_type` (`customer_type`),
  KEY `idx_customer_type_active` (`customer_type`,`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Customer information';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customers`
--

LOCK TABLES `customers` WRITE;
/*!40000 ALTER TABLE `customers` DISABLE KEYS */;
INSERT INTO `customers` VALUES (1,'Walk-in Customer',NULL,NULL,0.00,'walkin',1,'2026-02-14 11:37:11','2026-02-14 11:37:11',NULL),(2,'Malik Construction','03001112222','Model Town, Lahore',5015.90,'contractor',1,'2026-02-14 11:37:11','2026-02-23 16:52:03',NULL),(4,'Fazal Hardware Store','03217778888','Township, Lahore',0.00,'wholesale',1,'2026-02-14 11:37:11','2026-02-14 11:37:11',NULL),(6,'ahad','0909090','lahore',0.00,'retail',1,'2026-02-19 08:21:09','2026-02-19 08:21:09','hdjsh'),(7,'ABDUL AHAD ILYAS','03477048001','P/O same chak number 263 RB khan garden colony dijkot , Tehsil and district faisalabad',0.00,'retail',0,'2026-02-22 09:01:08','2026-02-22 09:01:24','');
/*!40000 ALTER TABLE `customers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `lookup`
--

DROP TABLE IF EXISTS `lookup`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `lookup` (
  `lookup_id` int NOT NULL AUTO_INCREMENT,
  `type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'category, payment_status, order_status, user_role, etc.',
  `value` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `display_order` int DEFAULT '0',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`lookup_id`),
  UNIQUE KEY `unique_lookup` (`type`,`value`),
  KEY `idx_type` (`type`),
  KEY `idx_active` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Reference data for dropdowns and statuses';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `lookup`
--

LOCK TABLES `lookup` WRITE;
/*!40000 ALTER TABLE `lookup` DISABLE KEYS */;
INSERT INTO `lookup` VALUES (1,'category','UPVC Pipes','UPVC pipes for plumbing',1,1,'2026-02-14 11:37:11'),(2,'category','Electrical Conduit','Electrical conduit pipes',2,1,'2026-02-14 11:37:11'),(3,'category','Pressure Pipes','High pressure pipes',3,1,'2026-02-14 11:37:11'),(4,'category','Pipe Fittings','Various pipe fittings and connectors',4,1,'2026-02-14 11:37:11'),(5,'category','Electrical Fittings','Electrical accessories and fittings',5,1,'2026-02-14 11:37:11'),(6,'category','Tools','Hardware tools',6,1,'2026-02-14 11:37:11'),(7,'category','Adhesives','Glue, tape, adhesives',7,1,'2026-02-14 11:37:11'),(8,'category','Fasteners','Nails, screws, bolts',8,1,'2026-02-14 11:37:11'),(9,'category','Other','Miscellaneous items',9,1,'2026-02-14 11:37:11'),(10,'payment_status','Pending','Payment not received',1,1,'2026-02-14 11:37:11'),(11,'payment_status','Partial','Partially paid',2,1,'2026-02-14 11:37:11'),(12,'payment_status','Paid','Fully paid',3,1,'2026-02-14 11:37:11'),(13,'payment_status','Overdue','Payment overdue',4,1,'2026-02-14 11:37:11'),(14,'payment_status','Refunded','Payment refunded (for returns)',5,1,'2026-02-14 11:37:11'),(15,'order_status','Pending','Order placed, not received',1,1,'2026-02-14 11:37:11'),(16,'order_status','Partial','Partially received',2,1,'2026-02-14 11:37:11'),(17,'order_status','Completed','Fully received',3,1,'2026-02-14 11:37:11'),(18,'order_status','Cancelled','Order cancelled',4,1,'2026-02-14 11:37:11'),(19,'quotation_status','Draft','Quotation being prepared',1,1,'2026-02-14 11:37:11'),(20,'quotation_status','Sent','Quotation sent to customer',2,1,'2026-02-14 11:37:11'),(21,'quotation_status','Accepted','Customer accepted quotation',3,1,'2026-02-14 11:37:11'),(22,'quotation_status','Rejected','Customer rejected quotation',4,1,'2026-02-14 11:37:11'),(23,'quotation_status','Converted','Converted to bill',5,1,'2026-02-14 11:37:11'),(24,'quotation_status','Expired','Quotation expired',6,1,'2026-02-14 11:37:11'),(25,'return_status','Pending','Return initiated, not approved',1,1,'2026-02-14 11:37:11'),(26,'return_status','Approved','Return approved, stock restored',2,1,'2026-02-14 11:37:11'),(27,'return_status','Rejected','Return rejected',3,1,'2026-02-14 11:37:11'),(28,'return_status','Completed','Return completed, refund issued',4,1,'2026-02-14 11:37:11'),(29,'user_role','Admin','Full system access',1,1,'2026-02-14 11:37:11'),(30,'user_role','Manager','Management level access',2,1,'2026-02-14 11:37:11'),(31,'user_role','Cashier','Point of sale operator',3,1,'2026-02-14 11:37:11'),(32,'user_role','Inventory','Inventory management',4,1,'2026-02-14 11:37:11');
/*!40000 ALTER TABLE `lookup` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payments` (
  `payment_id` int NOT NULL AUTO_INCREMENT,
  `bill_id` int DEFAULT NULL,
  `customer_id` int NOT NULL,
  `amount` decimal(12,2) NOT NULL,
  `payment_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `payment_method` enum('cash','card','bank_transfer','cheque') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `reference_number` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `received_by` int NOT NULL,
  PRIMARY KEY (`payment_id`),
  KEY `received_by` (`received_by`),
  KEY `idx_bill` (`bill_id`),
  KEY `idx_customer` (`customer_id`),
  KEY `idx_date` (`payment_date`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`bill_id`) REFERENCES `bills` (`bill_id`),
  CONSTRAINT `payments_ibfk_2` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  CONSTRAINT `payments_ibfk_3` FOREIGN KEY (`received_by`) REFERENCES `staff` (`staff_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
/*!40000 ALTER TABLE `payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `product_variants`
--

DROP TABLE IF EXISTS `product_variants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_variants` (
  `variant_id` int NOT NULL AUTO_INCREMENT,
  `product_id` int NOT NULL,
  `size` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT 'Standard' COMMENT 'Size or "Standard" for products without variants',
  `class_type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'e.g., Class 0, PN-1.6, PN-20, NULL for simple products',
  `unit_of_measure` enum('FT','LENGTH','PCS','MTR','PACK','UNIT','BOTTLE','BOX','KG','LITER') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'PCS',
  `price_per_unit` decimal(10,2) NOT NULL DEFAULT '0.00' COMMENT 'Price per FT/MTR/PCS/etc',
  `price_per_length` decimal(10,2) DEFAULT '0.00' COMMENT 'Price for full length (e.g., 13FT pipe)',
  `length_in_feet` decimal(5,2) DEFAULT '0.00' COMMENT 'Standard length if sold by piece',
  `quantity_in_stock` decimal(10,2) DEFAULT '0.00',
  `reorder_level` decimal(10,2) DEFAULT '0.00',
  `minimum_order_qty` decimal(10,2) DEFAULT '1.00',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`variant_id`),
  UNIQUE KEY `unique_variant` (`product_id`,`size`,`class_type`),
  KEY `idx_size` (`size`),
  KEY `idx_stock` (`quantity_in_stock`),
  KEY `idx_active` (`is_active`),
  KEY `idx_variant_stock_level` (`quantity_in_stock`,`reorder_level`),
  CONSTRAINT `product_variants_ibfk_1` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Product size/variant specific data including pricing and inventory';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `product_variants`
--

LOCK TABLES `product_variants` WRITE;
/*!40000 ALTER TABLE `product_variants` DISABLE KEYS */;
INSERT INTO `product_variants` VALUES (1,1,'1 1/2\"','CLASS \"0\"','FT',145.30,1888.90,13.00,3.00,10.00,1.00,1,'2026-02-14 11:37:12','2026-02-21 06:17:26'),(2,1,'2\"','CLASS \"0\"','FT',188.10,2445.30,13.00,226.00,52.00,1.00,1,'2026-02-14 11:37:12','2026-02-21 11:40:38'),(3,1,'3\"','CLASS \"0\"','FT',290.60,3777.80,13.00,75.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-14 11:37:12'),(4,2,'Standard',NULL,'BOTTLE',150.00,0.00,0.00,187.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-16 18:53:55'),(5,3,'Standard','class 0','PCS',238.00,0.00,0.00,100.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-14 12:35:50'),(6,4,'Standard',NULL,'PCS',120.00,0.00,0.00,150.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-14 11:37:12'),(7,5,'1 inch',NULL,'KG',200.00,0.00,0.00,50.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-14 11:37:12'),(8,5,'2 inch',NULL,'KG',220.00,0.00,0.00,140.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-21 11:40:38'),(9,5,'3 inch',NULL,'KG',250.00,0.00,0.00,30.00,0.00,1.00,1,'2026-02-14 11:37:12','2026-02-14 11:37:12'),(11,3,'Simple','class 0','PCS',238.00,0.00,0.00,100.00,0.00,1.00,1,'2026-02-14 12:36:51','2026-02-14 12:36:51');
/*!40000 ALTER TABLE `product_variants` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `products`
--

DROP TABLE IF EXISTS `products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `products` (
  `product_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `category_id` int NOT NULL,
  `description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `supplier_id` int DEFAULT NULL,
  `has_variants` tinyint(1) DEFAULT '1' COMMENT 'FALSE for simple products like glue',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`product_id`),
  KEY `idx_category` (`category_id`),
  KEY `idx_supplier` (`supplier_id`),
  KEY `idx_active` (`is_active`),
  KEY `idx_name` (`name`),
  KEY `idx_has_variants` (`has_variants`),
  KEY `idx_product_category_active` (`category_id`,`is_active`),
  CONSTRAINT `products_ibfk_1` FOREIGN KEY (`category_id`) REFERENCES `lookup` (`lookup_id`),
  CONSTRAINT `products_ibfk_2` FOREIGN KEY (`supplier_id`) REFERENCES `supplier` (`supplier_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Product master table';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
INSERT INTO `products` VALUES (1,'UPVC Pipe AS PER 135-3506',1,'Popular UPVC Pipes as per 135-3506 Class \"0\" (White)',3,1,1,'2026-02-14 11:37:12','2026-02-22 06:58:11'),(2,'Elephant Glue',7,'General purpose adhesive glue',4,0,1,'2026-02-14 11:37:12','2026-02-14 11:37:12'),(3,'Duct Tape',7,'Heavy duty duct tape roll',4,0,0,'2026-02-14 11:37:12','2026-02-14 15:10:20'),(4,'Masking Tape',7,'Painting masking tape',4,0,1,'2026-02-14 11:37:12','2026-02-14 11:37:12'),(5,'Steel Nails',8,'Steel construction nails',4,1,0,'2026-02-14 11:37:12','2026-02-22 06:59:13'),(6,'pipe',6,'General purpose pipe',4,1,1,'2026-02-14 12:38:23','2026-02-14 12:38:23');
/*!40000 ALTER TABLE `products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `purchase_batch_items`
--

DROP TABLE IF EXISTS `purchase_batch_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `purchase_batch_items` (
  `purchase_batch_item_id` int NOT NULL,
  `purchase_batch_id` int NOT NULL,
  `variant_id` int NOT NULL,
  `quantity_recieved` decimal(10,2) NOT NULL,
  `cost_price` decimal(10,2) NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`purchase_batch_item_id`),
  KEY `fk-variant_idx` (`variant_id`),
  CONSTRAINT `fk-variant` FOREIGN KEY (`variant_id`) REFERENCES `product_variants` (`variant_id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `purchase_batch_items`
--

LOCK TABLES `purchase_batch_items` WRITE;
/*!40000 ALTER TABLE `purchase_batch_items` DISABLE KEYS */;
INSERT INTO `purchase_batch_items` VALUES (1,1,1,80.00,12.00,'2026-02-14 21:17:01'),(2,2,2,1.00,160.00,'2026-02-14 21:30:02'),(3,3,1,20.00,130.00,'2026-02-15 01:52:11'),(4,4,2,125.00,170.00,'2026-02-18 10:57:48'),(5,5,2,50.00,150.00,'2026-02-21 16:40:38'),(6,5,8,100.00,75.00,'2026-02-21 16:40:38');
/*!40000 ALTER TABLE `purchase_batch_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `purchase_batches`
--

DROP TABLE IF EXISTS `purchase_batches`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `purchase_batches` (
  `batch_id` int NOT NULL,
  `supplier_id` int NOT NULL,
  `BatchName` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `total_price` decimal(10,2) NOT NULL,
  `paid` decimal(10,2) NOT NULL DEFAULT '0.00',
  `status` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`batch_id`),
  KEY `fk-supplier_idx` (`supplier_id`),
  CONSTRAINT `fk-supplier` FOREIGN KEY (`supplier_id`) REFERENCES `supplier` (`supplier_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `purchase_batches`
--

LOCK TABLES `purchase_batches` WRITE;
/*!40000 ALTER TABLE `purchase_batches` DISABLE KEYS */;
INSERT INTO `purchase_batches` VALUES (1,4,'h342',960.00,920.00,'Partial','2026-02-15 02:10:00'),(2,2,'jksnjds',200.00,220.00,'Completed','2026-02-15 02:10:00'),(3,2,'batch123',2600.00,2580.00,'Completed','2026-02-15 02:10:00'),(4,3,'feb-26-1',21250.00,10000.00,'Pending','2026-02-18 10:57:48'),(5,1,'February 2026 - Pipes Restock',15000.00,10000.00,'Partial','2026-02-21 16:40:38');
/*!40000 ALTER TABLE `purchase_batches` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `quotation_items`
--

DROP TABLE IF EXISTS `quotation_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `quotation_items` (
  `quotation_item_id` int NOT NULL AUTO_INCREMENT,
  `quotation_id` int NOT NULL,
  `product_id` int NOT NULL,
  `variant_id` int NOT NULL,
  `quantity` decimal(10,2) NOT NULL,
  `unit_of_measure` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `unit_price` decimal(10,2) NOT NULL,
  `line_total` decimal(12,2) NOT NULL,
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`quotation_item_id`),
  KEY `idx_quotation` (`quotation_id`),
  KEY `idx_product` (`product_id`),
  KEY `idx_variant` (`variant_id`),
  CONSTRAINT `quotation_items_ibfk_1` FOREIGN KEY (`quotation_id`) REFERENCES `quotations` (`quotation_id`) ON DELETE CASCADE,
  CONSTRAINT `quotation_items_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`),
  CONSTRAINT `quotation_items_ibfk_3` FOREIGN KEY (`variant_id`) REFERENCES `product_variants` (`variant_id`),
  CONSTRAINT `chk_quotation_item_price` CHECK ((`unit_price` >= 0)),
  CONSTRAINT `chk_quotation_item_qty` CHECK ((`quantity` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quotation line items - Does NOT reduce stock';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `quotation_items`
--

LOCK TABLES `quotation_items` WRITE;
/*!40000 ALTER TABLE `quotation_items` DISABLE KEYS */;
INSERT INTO `quotation_items` VALUES (1,2,1,1,1.00,'FT',145.30,145.30,NULL),(2,3,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(3,3,1,1,3.00,'FT',145.30,435.90,NULL),(4,4,1,1,1.00,'FT',145.30,145.30,NULL),(5,5,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(6,6,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(7,7,2,4,1.00,'BOTTLE',150.00,150.00,NULL),(8,8,2,4,2.00,'BOTTLE',150.00,300.00,NULL),(9,8,1,1,10.00,'FT',145.30,1453.00,NULL);
/*!40000 ALTER TABLE `quotation_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `quotations`
--

DROP TABLE IF EXISTS `quotations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `quotations` (
  `quotation_id` int NOT NULL AUTO_INCREMENT,
  `quotation_number` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'e.g., QUO-2024-0001',
  `customer_id` int DEFAULT NULL,
  `staff_id` int NOT NULL,
  `quotation_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `valid_until` date DEFAULT NULL COMMENT 'Quotation validity date',
  `subtotal` decimal(12,2) NOT NULL DEFAULT '0.00',
  `discount_percentage` decimal(5,2) DEFAULT '0.00',
  `discount_amount` decimal(12,2) DEFAULT '0.00',
  `tax_percentage` decimal(5,2) DEFAULT '0.00',
  `tax_amount` decimal(12,2) DEFAULT '0.00',
  `total_amount` decimal(12,2) NOT NULL DEFAULT '0.00',
  `status_id` int NOT NULL,
  `converted_bill_id` int DEFAULT NULL COMMENT 'Reference to bill if converted',
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `terms_conditions` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`quotation_id`),
  UNIQUE KEY `quotation_number` (`quotation_number`),
  KEY `staff_id` (`staff_id`),
  KEY `idx_quotation_number` (`quotation_number`),
  KEY `idx_customer` (`customer_id`),
  KEY `idx_status` (`status_id`),
  KEY `idx_date` (`quotation_date`),
  KEY `idx_quotation_date_status` (`quotation_date`,`status_id`),
  CONSTRAINT `quotations_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  CONSTRAINT `quotations_ibfk_2` FOREIGN KEY (`staff_id`) REFERENCES `staff` (`staff_id`),
  CONSTRAINT `quotations_ibfk_3` FOREIGN KEY (`status_id`) REFERENCES `lookup` (`lookup_id`),
  CONSTRAINT `chk_quotation_amounts` CHECK (((`subtotal` >= 0) and (`total_amount` >= 0)))
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quotations - Does NOT affect inventory';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `quotations`
--

LOCK TABLES `quotations` WRITE;
/*!40000 ALTER TABLE `quotations` DISABLE KEYS */;
INSERT INTO `quotations` VALUES (1,'QUO-2026-0001',2,2,'2026-02-14 11:37:14','2026-03-16',0.00,0.00,0.00,0.00,0.00,5000.00,19,NULL,NULL,NULL,'2026-02-14 11:37:14','2026-02-14 11:37:14'),(2,'QUO-2026-0216104928',1,1,'2026-02-16 05:49:07','2026-03-18',145.30,0.00,0.00,0.00,0.00,145.30,19,NULL,NULL,NULL,'2026-02-16 05:49:28','2026-02-16 05:49:28'),(3,'QUO-2026-0216232343',1,1,'2026-02-16 18:23:01','2026-03-18',700.90,0.00,0.00,0.00,0.00,700.90,19,NULL,NULL,NULL,'2026-02-16 18:23:43','2026-02-16 18:23:43'),(4,'QUO-2026-0217010057',1,1,'2026-02-16 20:00:39','2026-03-19',145.30,0.00,0.00,0.00,0.00,145.30,19,NULL,NULL,NULL,'2026-02-16 20:00:57','2026-02-16 20:00:57'),(5,'QUO-2026-0217173322',1,1,'2026-02-17 12:32:56','2026-03-19',150.00,0.00,0.00,0.00,0.00,150.00,19,NULL,NULL,NULL,'2026-02-17 12:33:22','2026-02-17 12:33:22'),(6,'QUO-2026-0217174408',1,1,'2026-02-17 12:43:59','2026-03-19',150.00,0.00,0.00,0.00,0.00,150.00,19,NULL,NULL,NULL,'2026-02-17 12:44:09','2026-02-17 12:44:09'),(7,'QUO-2026-0217174901',1,1,'2026-02-17 12:48:43','2026-03-19',150.00,0.00,0.00,0.00,0.00,150.00,19,NULL,NULL,NULL,'2026-02-17 12:49:01','2026-02-17 12:49:01'),(8,'QUO-2026-0223215327',1,1,'2026-02-23 16:52:49','2026-03-25',1753.00,0.00,0.00,0.00,0.00,1753.00,19,NULL,NULL,NULL,'2026-02-23 16:53:27','2026-02-23 16:53:27');
/*!40000 ALTER TABLE `quotations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `return_items`
--

DROP TABLE IF EXISTS `return_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `return_items` (
  `return_item_id` int NOT NULL AUTO_INCREMENT,
  `return_id` int NOT NULL,
  `variant_id` int NOT NULL,
  `quantity` decimal(10,2) NOT NULL,
  `condition_note` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Condition of returned item',
  PRIMARY KEY (`return_item_id`),
  KEY `idx_return` (`return_id`),
  KEY `idx_variant` (`variant_id`),
  CONSTRAINT `return_items_ibfk_1` FOREIGN KEY (`return_id`) REFERENCES `returns` (`return_id`) ON DELETE CASCADE,
  CONSTRAINT `return_items_ibfk_3` FOREIGN KEY (`variant_id`) REFERENCES `product_variants` (`variant_id`),
  CONSTRAINT `chk_return_item_qty` CHECK ((`quantity` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Return line items - Stock RESTORED via trigger when approved';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `return_items`
--

LOCK TABLES `return_items` WRITE;
/*!40000 ALTER TABLE `return_items` DISABLE KEYS */;
INSERT INTO `return_items` VALUES (2,3,1,2.00,'Resalable');
/*!40000 ALTER TABLE `return_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `returns`
--

DROP TABLE IF EXISTS `returns`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `returns` (
  `return_id` int NOT NULL AUTO_INCREMENT,
  `bill_id` int DEFAULT NULL COMMENT 'Original bill (if applicable)',
  `customer_id` int DEFAULT NULL,
  `return_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `refund_amount` decimal(12,2) DEFAULT '0.00' COMMENT 'Amount refunded to customer',
  `status_id` int NOT NULL,
  `reason` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Reason for return',
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`return_id`),
  KEY `idx_bill` (`bill_id`),
  KEY `idx_customer` (`customer_id`),
  KEY `idx_date` (`return_date`),
  CONSTRAINT `returns_ibfk_1` FOREIGN KEY (`bill_id`) REFERENCES `bills` (`bill_id`),
  CONSTRAINT `returns_ibfk_3` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Product returns - stock RESTORED via trigger';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `returns`
--

LOCK TABLES `returns` WRITE;
/*!40000 ALTER TABLE `returns` DISABLE KEYS */;
INSERT INTO `returns` VALUES (3,15,1,'2026-02-21 06:17:26',690.50,26,'Product defective - customer complaint','Items inspected - approved for return','2026-02-21 06:17:26','2026-02-21 06:17:26');
/*!40000 ALTER TABLE `returns` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `staff`
--

DROP TABLE IF EXISTS `staff`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `staff` (
  `staff_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `contact` varchar(15) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `cnic` char(13) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `role_id` int NOT NULL,
  `username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `password_hash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `hire_date` date DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `last_login` datetime DEFAULT NULL,
  PRIMARY KEY (`staff_id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `cnic` (`cnic`),
  KEY `role_id` (`role_id`),
  KEY `idx_username` (`username`),
  KEY `idx_active` (`is_active`),
  CONSTRAINT `staff_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `lookup` (`lookup_id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Staff and user accounts';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `staff`
--

LOCK TABLES `staff` WRITE;
/*!40000 ALTER TABLE `staff` DISABLE KEYS */;
INSERT INTO `staff` VALUES (1,'Admin User','admin@bismillah.com','03001234567','3520112345678',NULL,29,'admin','admin123',1,'2024-01-01','2026-02-14 11:37:11','2026-02-22 10:53:13',NULL),(2,'Muhammad Ali','ali@bismillah.com','03009876543','3520298765432',NULL,31,'cashier1','$2y$10$abcdefghijklmnopqrstuvwxyz123456789',1,'2024-06-01','2026-02-14 11:37:11','2026-02-14 11:37:11',NULL),(3,'Ahmed Khan','ahmed@bismillah.com','03111234567','3520387654321',NULL,30,'manager1','manager123',1,'2024-03-15','2026-02-14 11:37:11','2026-02-22 10:53:26',NULL),(10,'Super Administrator','superadmin@hardwarestore.com','03001111111','3520111111111',NULL,29,'superadmin','Yq8K5tJ9nP3vR7wL2mX6hF4dS1gZ8cB0aE5uT9iO3nM=',1,'2026-02-24','2026-02-24 09:54:11','2026-02-24 09:54:11',NULL);
/*!40000 ALTER TABLE `staff` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `supplier`
--

DROP TABLE IF EXISTS `supplier`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `supplier` (
  `supplier_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `contact` varchar(15) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `account_balance` decimal(12,2) DEFAULT '0.00',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `notes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`supplier_id`),
  KEY `idx_active` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Supplier information';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `supplier`
--

LOCK TABLES `supplier` WRITE;
/*!40000 ALTER TABLE `supplier` DISABLE KEYS */;
INSERT INTO `supplier` VALUES (1,'Popular Pipes Ltd.','04235678901','Industrial Area, Lahore',0.00,0,'2026-02-14 11:37:11','2026-02-19 09:18:10',NULL),(2,'Diamond Fittings Co.','04237890123','Shahdara, Lahore',2580.00,1,'2026-02-14 11:37:11','2026-02-14 21:16:16',NULL),(3,'Elite Electricals','04239012345','Raiwind Road, Lahore',0.00,1,'2026-02-14 11:37:11','2026-02-14 11:37:11',NULL),(4,'General Supplies','04231234567','Johar Town, Lahore',820.00,1,'2026-02-14 11:37:11','2026-02-14 21:15:58',NULL),(5,'ibs','1212','assa',0.00,1,'2026-02-16 19:52:09','2026-02-16 19:52:09',NULL),(6,'ABC Supplies Ltd','03001234567','Industrial Area, Lahore',0.00,1,'2026-02-19 09:17:52','2026-02-19 09:17:52','Reliable supplier');
/*!40000 ALTER TABLE `supplier` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `supplier_payment_records`
--

DROP TABLE IF EXISTS `supplier_payment_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `supplier_payment_records` (
  `payment_id` int NOT NULL AUTO_INCREMENT,
  `supplier_id` int NOT NULL,
  `batch_id` int NOT NULL,
  `payment_amount` decimal(12,2) NOT NULL,
  `payment_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `remarks` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`payment_id`),
  KEY `idx_supplier` (`supplier_id`),
  KEY `idx_batch` (`batch_id`),
  KEY `idx_payment_date` (`payment_date`),
  CONSTRAINT `fk_payment_batch` FOREIGN KEY (`batch_id`) REFERENCES `purchase_batches` (`batch_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_payment_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `supplier` (`supplier_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Supplier payment records for purchase batches';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `supplier_payment_records`
--

LOCK TABLES `supplier_payment_records` WRITE;
/*!40000 ALTER TABLE `supplier_payment_records` DISABLE KEYS */;
INSERT INTO `supplier_payment_records` VALUES (1,2,2,200.00,'2026-02-15 01:41:21','ndsndsndns','2026-02-14 20:41:32'),(2,4,1,800.00,'2026-02-15 01:50:10','2nd payment','2026-02-14 20:50:24'),(3,4,1,20.00,'2026-02-15 02:15:51','nsndnnds','2026-02-14 21:15:58'),(4,2,3,2380.00,'2026-02-15 02:16:04','2300','2026-02-14 21:16:16');
/*!40000 ALTER TABLE `supplier_payment_records` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary view structure for view `v_low_stock_alerts`
--

DROP TABLE IF EXISTS `v_low_stock_alerts`;
/*!50001 DROP VIEW IF EXISTS `v_low_stock_alerts`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `v_low_stock_alerts` AS SELECT 
 1 AS `product_id`,
 1 AS `product_name`,
 1 AS `category`,
 1 AS `variant_id`,
 1 AS `size`,
 1 AS `class_type`,
 1 AS `quantity_in_stock`,
 1 AS `reorder_level`,
 1 AS `quantity_needed`,
 1 AS `supplier_name`,
 1 AS `supplier_contact`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `v_product_stock_summary`
--

DROP TABLE IF EXISTS `v_product_stock_summary`;
/*!50001 DROP VIEW IF EXISTS `v_product_stock_summary`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `v_product_stock_summary` AS SELECT 
 1 AS `product_id`,
 1 AS `product_name`,
 1 AS `category`,
 1 AS `has_variants`,
 1 AS `variant_id`,
 1 AS `size`,
 1 AS `class_type`,
 1 AS `unit_of_measure`,
 1 AS `price_per_unit`,
 1 AS `price_per_length`,
 1 AS `quantity_in_stock`,
 1 AS `reorder_level`,
 1 AS `supplier_name`,
 1 AS `stock_status`,
 1 AS `last_updated`*/;
SET character_set_client = @saved_cs_client;

--
-- Final view structure for view `v_low_stock_alerts`
--

/*!50001 DROP VIEW IF EXISTS `v_low_stock_alerts`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `v_low_stock_alerts` AS select `p`.`product_id` AS `product_id`,`p`.`name` AS `product_name`,`l`.`value` AS `category`,`pv`.`variant_id` AS `variant_id`,`pv`.`size` AS `size`,`pv`.`class_type` AS `class_type`,`pv`.`quantity_in_stock` AS `quantity_in_stock`,`pv`.`reorder_level` AS `reorder_level`,(`pv`.`reorder_level` - `pv`.`quantity_in_stock`) AS `quantity_needed`,`s`.`name` AS `supplier_name`,`s`.`contact` AS `supplier_contact` from (((`products` `p` join `product_variants` `pv` on((`p`.`product_id` = `pv`.`product_id`))) join `lookup` `l` on((`p`.`category_id` = `l`.`lookup_id`))) left join `supplier` `s` on((`p`.`supplier_id` = `s`.`supplier_id`))) where ((`pv`.`quantity_in_stock` <= `pv`.`reorder_level`) and (`p`.`is_active` = true) and (`pv`.`is_active` = true)) order by (`pv`.`reorder_level` - `pv`.`quantity_in_stock`) desc */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `v_product_stock_summary`
--

/*!50001 DROP VIEW IF EXISTS `v_product_stock_summary`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `v_product_stock_summary` AS select `p`.`product_id` AS `product_id`,`p`.`name` AS `product_name`,`l`.`value` AS `category`,`p`.`has_variants` AS `has_variants`,`pv`.`variant_id` AS `variant_id`,`pv`.`size` AS `size`,`pv`.`class_type` AS `class_type`,`pv`.`unit_of_measure` AS `unit_of_measure`,`pv`.`price_per_unit` AS `price_per_unit`,`pv`.`price_per_length` AS `price_per_length`,`pv`.`quantity_in_stock` AS `quantity_in_stock`,`pv`.`reorder_level` AS `reorder_level`,`s`.`name` AS `supplier_name`,(case when (`pv`.`quantity_in_stock` <= `pv`.`reorder_level`) then 'Low Stock' when (`pv`.`quantity_in_stock` = 0) then 'Out of Stock' else 'In Stock' end) AS `stock_status`,`pv`.`updated_at` AS `last_updated` from (((`products` `p` join `product_variants` `pv` on((`p`.`product_id` = `pv`.`product_id`))) join `lookup` `l` on((`p`.`category_id` = `l`.`lookup_id`))) left join `supplier` `s` on((`p`.`supplier_id` = `s`.`supplier_id`))) where ((`p`.`is_active` = true) and (`pv`.`is_active` = true)) order by `p`.`name`,`pv`.`size` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-02-24 23:18:32
