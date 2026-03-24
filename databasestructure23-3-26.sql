-- MySQL dump 10.13  Distrib 8.0.42, for Win64 (x86_64)
--
-- Host: localhost    Database: fertilizernew
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
-- Table structure for table `batch_details`
--

DROP TABLE IF EXISTS `batch_details`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `batch_details` (
  `batch_detail_id` int NOT NULL AUTO_INCREMENT,
  `batch_id` int NOT NULL,
  `product_id` int NOT NULL,
  `cost_price` int NOT NULL,
  `quantity_recived` int NOT NULL,
  PRIMARY KEY (`batch_detail_id`),
  KEY `batch_id` (`batch_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `batch_details_ibfk_1` FOREIGN KEY (`batch_id`) REFERENCES `batches` (`batch_id`) ON DELETE CASCADE,
  CONSTRAINT `batch_details_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `batches`
--

DROP TABLE IF EXISTS `batches`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `batches` (
  `batch_id` int NOT NULL AUTO_INCREMENT,
  `batch_name` varchar(100) NOT NULL,
  `supplier_id` int NOT NULL,
  `recieved_date` datetime DEFAULT NULL,
  PRIMARY KEY (`batch_id`),
  KEY `supplier_id` (`supplier_id`),
  CONSTRAINT `batches_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`supplier_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_bill_details`
--

DROP TABLE IF EXISTS `customer_bill_details`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customer_bill_details` (
  `Bill_detail_ID` int NOT NULL AUTO_INCREMENT,
  `Bill_id` int NOT NULL,
  `product_id` int DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  `discount` int DEFAULT '0',
  `status` enum('bill','invoice') NOT NULL,
  PRIMARY KEY (`Bill_detail_ID`),
  KEY `customerbilldetail_ibfk_2` (`product_id`),
  KEY `customerbilldetail_ibfk_1` (`Bill_id`),
  CONSTRAINT `customerbilldetail_ibfk_1` FOREIGN KEY (`Bill_id`) REFERENCES `customerbills` (`BillID`) ON DELETE CASCADE,
  CONSTRAINT `customerbilldetail_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=52 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_return_items`
--

DROP TABLE IF EXISTS `customer_return_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customer_return_items` (
  `return_item_id` int NOT NULL AUTO_INCREMENT,
  `bill_id` int NOT NULL,
  `return_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `product_id` int NOT NULL,
  `quantity_returned` int NOT NULL,
  `refund_amount` decimal(10,2) NOT NULL,
  PRIMARY KEY (`return_item_id`),
  KEY `bill_id` (`bill_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `fk_return_items_bill` FOREIGN KEY (`bill_id`) REFERENCES `customerbills` (`BillID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_return_items_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customerbills`
--

DROP TABLE IF EXISTS `customerbills`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customerbills` (
  `BillID` int NOT NULL AUTO_INCREMENT,
  `CustomerID` int NOT NULL,
  `SaleDate` date NOT NULL,
  `total_price` int DEFAULT NULL,
  `paid_amount` int DEFAULT NULL,
  `payment_status` enum('Paid','Due') DEFAULT 'Due',
  PRIMARY KEY (`BillID`),
  KEY `CustomerID` (`CustomerID`),
  CONSTRAINT `customerbills_ibfk_1` FOREIGN KEY (`CustomerID`) REFERENCES `customers` (`customer_id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customerpricerecord`
--

DROP TABLE IF EXISTS `customerpricerecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customerpricerecord` (
  `record_id` int NOT NULL AUTO_INCREMENT,
  `customer_id` int NOT NULL,
  `BillID` int NOT NULL,
  `date` date NOT NULL,
  `payment` decimal(10,2) NOT NULL,
  `remarks` varchar(255) DEFAULT 'No remarks',
  PRIMARY KEY (`record_id`),
  KEY `customer_id` (`customer_id`),
  KEY `BillID` (`BillID`),
  CONSTRAINT `customerpricerecord_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`customer_id`),
  CONSTRAINT `customerpricerecord_ibfk_2` FOREIGN KEY (`BillID`) REFERENCES `customerbills` (`BillID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=40 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customers`
--

DROP TABLE IF EXISTS `customers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customers` (
  `customer_id` int NOT NULL AUTO_INCREMENT,
  `first_name` varchar(50) DEFAULT NULL,
  `last_name` varchar(50) DEFAULT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`customer_id`),
  UNIQUE KEY `first_name` (`first_name`,`last_name`),
  UNIQUE KEY `unique_name` (`first_name`,`last_name`),
  UNIQUE KEY `phone_UNIQUE` (`phone`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_log`
--

DROP TABLE IF EXISTS `inventory_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `inventory_log` (
  `log_id` int NOT NULL AUTO_INCREMENT,
  `product_id` int NOT NULL,
  `change_type` enum('purchase','sale','return','return_to_supplier','manual_adjustment','used_in_service') NOT NULL,
  `quantity_change` int NOT NULL,
  `log_date` datetime DEFAULT CURRENT_TIMESTAMP,
  `remarks` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`log_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `inventory_log_ibfk_1` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `new_table`
--

DROP TABLE IF EXISTS `new_table`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `new_table` (
  `bank_id` int NOT NULL AUTO_INCREMENT,
  `bank_name` varchar(45) NOT NULL,
  `remaining_Balance` decimal(10,2) NOT NULL DEFAULT '0.00',
  PRIMARY KEY (`bank_id`),
  UNIQUE KEY `bank_name_UNIQUE` (`bank_name`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `orderdetails`
--

DROP TABLE IF EXISTS `orderdetails`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderdetails` (
  `orderdetail_id` int NOT NULL AUTO_INCREMENT,
  `order_id` int DEFAULT NULL,
  `product_id` int DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  PRIMARY KEY (`orderdetail_id`),
  KEY `order_id` (`order_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `orderdetails_ibfk_1` FOREIGN KEY (`order_id`) REFERENCES `orders` (`order_id`),
  CONSTRAINT `orderdetails_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `order_id` int NOT NULL AUTO_INCREMENT,
  `supplier_id` int DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `order_status` enum('completed','pending') NOT NULL DEFAULT 'pending',
  PRIMARY KEY (`order_id`),
  KEY `supplier_id` (`supplier_id`),
  CONSTRAINT `orders_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`supplier_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `products`
--

DROP TABLE IF EXISTS `products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `products` (
  `product_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` varchar(200) DEFAULT NULL,
  `sale_price` int DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  PRIMARY KEY (`product_id`),
  UNIQUE KEY `unique_name_description` (`name`,`description`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_bill_details`
--

DROP TABLE IF EXISTS `supplier_bill_details`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `supplier_bill_details` (
  `s_Bill_detail_ID` int NOT NULL AUTO_INCREMENT,
  `supplier_Bill_ID` int NOT NULL,
  `product_id` int DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  PRIMARY KEY (`s_Bill_detail_ID`),
  KEY `supplierbilldetail_ibfk_2` (`product_id`),
  KEY `supplierbilldetail_ibfk_1` (`supplier_Bill_ID`),
  CONSTRAINT `supplierbilldetail_ibfk_1` FOREIGN KEY (`supplier_Bill_ID`) REFERENCES `supplierbills` (`supplier_Bill_ID`) ON UPDATE CASCADE,
  CONSTRAINT `supplierbilldetail_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplierbills`
--

DROP TABLE IF EXISTS `supplierbills`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `supplierbills` (
  `supplier_Bill_ID` int NOT NULL AUTO_INCREMENT,
  `supplier_id` int NOT NULL,
  `Date` date NOT NULL,
  `total_price` int DEFAULT NULL,
  `batch_id` int DEFAULT NULL,
  `paid_amount` int DEFAULT '0',
  `payment_status` enum('Paid','Due') DEFAULT 'Due',
  PRIMARY KEY (`supplier_Bill_ID`),
  KEY `supplier_id` (`supplier_id`),
  KEY `fk_sb_2` (`batch_id`),
  CONSTRAINT `fk_sb_2` FOREIGN KEY (`batch_id`) REFERENCES `batches` (`batch_id`),
  CONSTRAINT `supplierbills_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`supplier_id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_paid_vs_total` CHECK ((`paid_amount` <= `total_price`)),
  CONSTRAINT `chk_supplier_paid_amount` CHECK ((`paid_amount` <= `total_price`))
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplierpricerecord`
--

DROP TABLE IF EXISTS `supplierpricerecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `supplierpricerecord` (
  `supplier_record_id` int NOT NULL AUTO_INCREMENT,
  `supplier_id` int NOT NULL,
  `supplier_Bill_ID` int NOT NULL,
  `date` date NOT NULL,
  `payment` decimal(10,2) NOT NULL,
  `remarks` varchar(255) DEFAULT 'No Remarks',
  PRIMARY KEY (`supplier_record_id`),
  KEY `supplier_id` (`supplier_id`),
  KEY `supplier_Bill_ID` (`supplier_Bill_ID`),
  CONSTRAINT `supplierpricerecord_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`supplier_id`),
  CONSTRAINT `supplierpricerecord_ibfk_2` FOREIGN KEY (`supplier_Bill_ID`) REFERENCES `supplierbills` (`supplier_Bill_ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `suppliers`
--

DROP TABLE IF EXISTS `suppliers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `suppliers` (
  `supplier_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`supplier_id`),
  UNIQUE KEY `name` (`name`),
  UNIQUE KEY `phone_UNIQUE` (`phone`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `transaction_history`
--

DROP TABLE IF EXISTS `transaction_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `transaction_history` (
  `record_id` int NOT NULL AUTO_INCREMENT,
  `bank_id` int NOT NULL,
  `transaction_type` enum('withdraw','deposit') NOT NULL,
  `date` datetime NOT NULL,
  `Amount` decimal(10,2) NOT NULL,
  PRIMARY KEY (`record_id`),
  KEY `fk_1_idx` (`bank_id`),
  CONSTRAINT `fk_1` FOREIGN KEY (`bank_id`) REFERENCES `new_table` (`bank_id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `full_name` varchar(100) DEFAULT NULL,
  `role` enum('Admin','Technician','Sales') NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  KEY `idx_users_username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-23 16:36:47
