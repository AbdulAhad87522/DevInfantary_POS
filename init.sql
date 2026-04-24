-- Create database if it doesn't exist
CREATE DATABASE IF NOT EXISTS harware;
USE harware;

-- MySQL dump 10.13  Distrib 8.0.42, for Win64 (x86_64)
--
-- Host: localhost    Database: hardwarewithdata
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
) ENGINE=InnoDB AUTO_INCREMENT=70 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bill line items - Stock REDUCED via trigger';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bill_items`
--

LOCK TABLES `bill_items` WRITE;
/*!40000 ALTER TABLE `bill_items` DISABLE KEYS */;
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
  `customer_id` int DEFAULT '1',
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
  CONSTRAINT `bills_ibfk_3` FOREIGN KEY (`staff_id`) REFERENCES `staff` (`staff_id`),
  CONSTRAINT `bills_ibfk_4` FOREIGN KEY (`payment_status_id`) REFERENCES `lookup` (`lookup_id`),
  CONSTRAINT `chk_bill_amounts` CHECK (((`subtotal` >= 0) and (`total_amount` >= 0)))
) ENGINE=InnoDB AUTO_INCREMENT=75 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bills/Invoices - REDUCES inventory when items added';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bills`
--

LOCK TABLES `bills` WRITE;
/*!40000 ALTER TABLE `bills` DISABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=174 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customerpricerecord`
--

LOCK TABLES `customerpricerecord` WRITE;
/*!40000 ALTER TABLE `customerpricerecord` DISABLE KEYS */;
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
  UNIQUE KEY `full_name_UNIQUE` (`full_name`),
  KEY `idx_contact` (`phone`),
  KEY `idx_active` (`is_active`),
  KEY `idx_type` (`customer_type`),
  KEY `idx_customer_type_active` (`customer_type`,`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Customer information';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customers`
--

LOCK TABLES `customers` WRITE;
/*!40000 ALTER TABLE `customers` DISABLE KEYS */;
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
  `display_order` int DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`lookup_id`),
  UNIQUE KEY `unique_lookup` (`type`,`value`),
  KEY `idx_type` (`type`),
  KEY `idx_active` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Reference data for dropdowns and statuses';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `lookup`
--

LOCK TABLES `lookup` WRITE;
/*!40000 ALTER TABLE `lookup` DISABLE KEYS */;
INSERT INTO `lookup` VALUES (1,'category','UPVC Pipes','UPVC pipes for plumbing',1,1,'2026-02-14 11:37:11'),(2,'category','Electrical Conduit','Electrical conduit pipes',2,1,'2026-02-14 11:37:11'),(3,'category','Pressure Pipes','High pressure pipes',3,1,'2026-02-14 11:37:11'),(4,'category','Pipe Fittings','Various pipe fittings and connectors',4,1,'2026-02-14 11:37:11'),(5,'category','Electrical Fittings','Electrical accessories and fittings',5,1,'2026-02-14 11:37:11'),(6,'category','Tools','Hardware tools',6,1,'2026-02-14 11:37:11'),(7,'category','Adhesives','Glue, tape, adhesives',7,1,'2026-02-14 11:37:11'),(8,'category','Fasteners','Nails, screws, bolts',8,1,'2026-02-14 11:37:11'),(9,'category','Other','Miscellaneous items',9,1,'2026-02-14 11:37:11'),(10,'payment_status','Pending','Payment not received',1,1,'2026-02-14 11:37:11'),(11,'payment_status','Partial','Partially paid',2,1,'2026-02-14 11:37:11'),(12,'payment_status','Paid','Fully paid',3,1,'2026-02-14 11:37:11'),(13,'payment_status','Overdue','Payment overdue',4,1,'2026-02-14 11:37:11'),(14,'payment_status','Refunded','Payment refunded (for returns)',5,1,'2026-02-14 11:37:11'),(15,'order_status','Pending','Order placed, not received',1,1,'2026-02-14 11:37:11'),(16,'order_status','Partial','Partially received',2,1,'2026-02-14 11:37:11'),(17,'order_status','Completed','Fully received',3,1,'2026-02-14 11:37:11'),(18,'order_status','Cancelled','Order cancelled',4,1,'2026-02-14 11:37:11'),(19,'quotation_status','Draft','Quotation being prepared',1,1,'2026-02-14 11:37:11'),(20,'quotation_status','Sent','Quotation sent to customer',2,1,'2026-02-14 11:37:11'),(21,'quotation_status','Accepted','Customer accepted quotation',3,1,'2026-02-14 11:37:11'),(22,'quotation_status','Rejected','Customer rejected quotation',4,1,'2026-02-14 11:37:11'),(23,'quotation_status','Converted','Converted to bill',5,1,'2026-02-14 11:37:11'),(24,'quotation_status','Expired','Quotation expired',6,1,'2026-02-14 11:37:11'),(25,'return_status','Pending','Return initiated, not approved',1,1,'2026-02-14 11:37:11'),(26,'return_status','Approved','Return approved, stock restored',2,1,'2026-02-14 11:37:11'),(27,'return_status','Rejected','Return rejected',3,1,'2026-02-14 11:37:11'),(28,'return_status','Completed','Return completed, refund issued',4,1,'2026-02-14 11:37:11'),(29,'user_role','Admin','Full system access',1,1,'2026-02-14 11:37:11'),(30,'user_role','Manager','Management level access',2,1,'2026-02-14 11:37:11'),(31,'user_role','Cashier','Point of sale operator',3,1,'2026-02-14 11:37:11'),(32,'user_role','Inventory','Inventory management',4,1,'2026-02-14 11:37:11'),(33,'category','goond','achi cheez',1,1,'2026-03-14 07:09:10'),(34,'category','piping',NULL,10,1,'2026-03-18 19:55:11');
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
) ENGINE=InnoDB AUTO_INCREMENT=67 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Product size/variant specific data including pricing and inventory';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `product_variants`
--

LOCK TABLES `product_variants` WRITE;
/*!40000 ALTER TABLE `product_variants` DISABLE KEYS */;
INSERT INTO `product_variants` VALUES (18,16,'1 1/2','Class 0','FT',0.00,145.30,0.00,0.00,50.00,1.00,1,'2026-03-23 14:58:05','2026-03-23 16:21:20'),(19,16,'2','Class 0','FT',0.00,188.10,0.00,0.00,50.00,1.00,1,'2026-03-23 15:01:46','2026-03-23 16:21:20'),(20,16,'3','Class 0','FT',0.00,290.60,0.00,0.00,50.00,1.00,1,'2026-03-23 15:02:24','2026-03-23 16:21:20'),(21,16,'4','Class 0','FT',0.00,418.80,0.00,0.00,50.00,1.00,1,'2026-03-23 15:02:49','2026-03-23 16:21:20'),(22,16,'5','Class 0','FT',0.00,598.30,0.00,0.00,50.00,1.00,1,'2026-03-23 15:03:27','2026-03-23 16:21:20'),(23,16,'6','Class 0','FT',0.00,923.00,0.00,0.00,50.00,1.00,1,'2026-03-23 15:04:09','2026-03-23 16:21:20'),(24,16,'8','Class 0','FT',0.00,1179.40,0.00,0.00,50.00,1.00,1,'2026-03-23 15:06:10','2026-03-23 16:21:20'),(25,16,'10','Class 00','FT',0.00,1581.10,0.00,0.00,50.00,1.00,1,'2026-03-23 15:06:37','2026-03-23 16:21:20'),(26,16,'12','Class 0','FT',0.00,2008.40,0.00,0.00,50.00,1.00,1,'2026-03-23 15:06:44','2026-03-23 16:21:20'),(27,16,'14','Class 0','FT',0.00,2393.00,0.00,0.00,50.00,1.00,1,'2026-03-23 15:06:49','2026-03-23 16:21:20'),(28,17,'2','SDR 32.5','FT',0.00,205.50,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:19','2026-03-23 16:34:50'),(29,17,'2','SDR 26','FT',0.00,240.00,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:24','2026-03-23 16:34:50'),(30,17,'3','SDR 41','FT',0.00,313.40,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:30','2026-03-23 16:34:50'),(31,17,'3','SDR 32.5','FT',0.00,414.70,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:34','2026-03-23 16:34:50'),(32,17,'4','SDR 64','FT',0.00,379.70,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:39','2026-03-23 16:34:50'),(33,17,'4','SDR 41','FT',0.00,557.60,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:42','2026-03-23 16:34:50'),(34,17,'4','SDR 32.5','FT',0.00,682.00,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:46','2026-03-23 16:34:50'),(35,17,'6','SDR 64','FT',0.00,774.10,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:51','2026-03-23 16:34:50'),(36,17,'6','50841','FT',0.00,1172.20,NULL,0.00,50.00,1.00,1,'2026-03-23 16:24:54','2026-03-23 16:34:50'),(37,17,'6','SDR 32.5','FT',0.00,1474.50,NULL,0.00,50.00,1.00,1,'2026-03-23 16:25:04','2026-03-23 16:34:50'),(38,17,'8','SDR 64','FT',0.00,1317.80,NULL,0.00,50.00,1.00,1,'2026-03-23 16:25:27','2026-03-23 16:34:50'),(39,17,'8','SDR 41','FT',0.00,1981.40,NULL,0.00,50.00,1.00,1,'2026-03-23 16:28:29','2026-03-23 16:37:56'),(40,17,'10','SOR 64','FT',0.00,1981.40,NULL,0.00,50.00,1.00,1,'2026-03-23 16:28:33','2026-03-23 16:37:56'),(41,17,'10','SDR 41','FT',0.00,3087.20,NULL,0.00,50.00,1.00,1,'2026-03-23 16:28:40','2026-03-23 16:37:56'),(42,17,'12','SDR 64','FT',0.00,2810.70,NULL,0.00,50.00,1.00,1,'2026-03-23 16:28:45','2026-03-23 16:37:56'),(43,17,'12','SDR 41','FT',0.00,4257.60,NULL,0.00,50.00,1.00,1,'2026-03-23 16:28:59','2026-03-23 16:37:56'),(44,18,'3','MEDIUM','FT',0.00,330.10,NULL,0.00,50.00,1.00,1,'2026-03-23 16:40:04','2026-03-23 16:42:09'),(45,18,'4','MEDIUM','FT',0.00,431.00,NULL,0.00,50.00,1.00,1,'2026-03-23 16:40:11','2026-03-23 16:42:09'),(46,18,'6','MEDIUM','FT',0.00,962.80,NULL,0.00,50.00,1.00,1,'2026-03-23 16:40:15','2026-03-23 16:42:09'),(47,18,'8','MEDIUM','FT',0.00,1604.70,NULL,0.00,50.00,1.00,1,'2026-03-23 16:40:22','2026-03-23 16:42:09'),(48,19,'1/2','U-PVC CONDUIT PIPE','FT',0.00,29.80,NULL,0.00,50.00,1.00,1,'2026-03-23 17:20:52','2026-03-23 17:27:47'),(49,19,'3/4','U-PVC CONDUIT PIPE','FT',0.00,41.80,NULL,0.00,50.00,1.00,1,'2026-03-23 17:20:58','2026-03-23 17:27:47'),(50,19,'1','U-PVC CONDUIT PIPE','FT',0.00,57.30,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:04','2026-03-23 17:27:47'),(51,19,'1 1/4','U-PVC CONDUIT PIPE','FT',0.00,78.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:09','2026-03-23 17:27:47'),(53,19,'1 1/2','U-PVC CONDUIT PIPE','FT',0.00,110.10,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:26','2026-03-23 17:27:47'),(54,19,'2','U-PVC CONDUIT PIPE','FT',0.00,155.10,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:31','2026-03-23 17:27:47'),(55,19,'3','U-PVC CONDUIT PIPE','FT',0.00,238.40,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:40','2026-03-23 17:27:47'),(56,19,'4','U-PVC CONDUIT PIPE','FT',0.00,330.10,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:45','2026-03-23 17:27:47'),(57,19,'5','U-PVC CONDUIT PIPE','FT',0.00,641.90,NULL,0.00,50.00,1.00,1,'2026-03-23 17:21:45','2026-03-23 17:21:45'),(58,20,'1/2','SOCKET','FT',0.00,9.70,NULL,0.00,50.00,1.00,1,'2026-03-23 17:28:43','2026-03-23 17:59:22'),(59,20,'3/4','SOCKET','FT',0.00,9.40,NULL,0.00,50.00,1.00,1,'2026-03-23 17:28:50','2026-03-23 17:59:22'),(60,20,'1','SOCKET','FT',0.00,17.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:28:55','2026-03-23 17:59:22'),(61,20,'11/4','SOCKET','FT',0.00,47.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:28:58','2026-03-23 17:59:22'),(62,20,'11/2','SOCKET','FT',0.00,63.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:29:02','2026-03-23 17:59:22'),(63,20,'2','SOCKET','FT',0.00,92.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:29:07','2026-03-23 17:59:22'),(64,20,'3','SOCKET','FT',0.00,156.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:29:11','2026-03-23 17:59:22'),(65,20,'4','SOCKET','FT',0.00,236.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:29:11','2026-03-23 17:29:11'),(66,20,'5','SOCKET','FT',0.00,557.00,NULL,0.00,50.00,1.00,1,'2026-03-23 17:29:11','2026-03-23 17:29:11');
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
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Product master table';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
INSERT INTO `products` VALUES (16,' POPULAR UPVC PIPES AS PER 135-3506 CLASS \"0\" (WHITE)',1,'Piping',NULL,1,1,'2026-03-23 11:45:38','2026-03-23 17:19:36'),(17,'POPULAR UPVC PIPES AS PER ASTM D-2241 SDR-SERIES (WHITE)',1,'pipes',NULL,1,1,'2026-03-23 16:22:12','2026-03-23 17:19:36'),(18,'POPULAR UPVC PIPES (GRAY)',1,'pipes',NULL,1,1,'2026-03-23 16:39:43','2026-03-23 17:19:36'),(19,'POPULAR UPVC ELECTRIC CONDUIT PIPES AS PER P5-1905 & B5-6099 (GREY)',2,'Electricity',NULL,1,1,'2026-03-23 17:19:55','2026-03-23 17:19:55'),(20,'POPULAR UPVC ELECTRIC CONDUIT FITTINGS RS. /Each (GREY)',2,'Electricity',NULL,1,1,'2026-03-23 17:28:14','2026-03-23 17:28:14');
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
  `updated_at` datetime DEFAULT NULL,
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
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quotation line items - Does NOT reduce stock';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `quotation_items`
--

LOCK TABLES `quotation_items` WRITE;
/*!40000 ALTER TABLE `quotation_items` DISABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=22 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quotations - Does NOT affect inventory';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `quotations`
--

LOCK TABLES `quotations` WRITE;
/*!40000 ALTER TABLE `quotations` DISABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Return line items - Stock RESTORED via trigger when approved';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `return_items`
--

LOCK TABLES `return_items` WRITE;
/*!40000 ALTER TABLE `return_items` DISABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Product returns - stock RESTORED via trigger';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `returns`
--

LOCK TABLES `returns` WRITE;
/*!40000 ALTER TABLE `returns` DISABLE KEYS */;
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
  UNIQUE KEY `name_UNIQUE` (`name`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `cnic` (`cnic`),
  KEY `role_id` (`role_id`),
  KEY `idx_username` (`username`),
  KEY `idx_active` (`is_active`),
  CONSTRAINT `staff_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `lookup` (`lookup_id`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Staff and user accounts';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `staff`
--

LOCK TABLES `staff` WRITE;
/*!40000 ALTER TABLE `staff` DISABLE KEYS */;
INSERT INTO `staff` VALUES (16,'abdul ahad ilyas','abdulahad18022@gamil.com','090909090','3310099887787','Mission Road',31,'ahmed','xXFriPfm06pMDw0dFwYEd2+dNkgbo9mceE71OeYWYRA=',1,'2026-03-23','2026-03-22 06:44:00','2026-03-22 06:44:00',NULL);
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
  UNIQUE KEY `name_UNIQUE` (`name`),
  KEY `idx_active` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Supplier information';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `supplier`
--

LOCK TABLES `supplier` WRITE;
/*!40000 ALTER TABLE `supplier` DISABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Supplier payment records for purchase batches';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `supplier_payment_records`
--

LOCK TABLES `supplier_payment_records` WRITE;
/*!40000 ALTER TABLE `supplier_payment_records` DISABLE KEYS */;
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

-- Dump completed on 2026-03-24 12:47:13
