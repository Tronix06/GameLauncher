CREATE DATABASE  IF NOT EXISTS `bitronix_db` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `bitronix_db`;
-- MySQL dump 10.13  Distrib 8.0.43, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: bitronix_db
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
-- Table structure for table `apelaciones`
--

DROP TABLE IF EXISTS `apelaciones`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apelaciones` (
  `id` int NOT NULL AUTO_INCREMENT,
  `usuario_afectado_id` int DEFAULT NULL,
  `mensaje_apelacion` text,
  `estado` enum('pendiente','aceptada','rechazada') DEFAULT 'pendiente',
  `fecha_solicitud` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `usuario_afectado_id` (`usuario_afectado_id`),
  CONSTRAINT `apelaciones_ibfk_1` FOREIGN KEY (`usuario_afectado_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apelaciones`
--

LOCK TABLES `apelaciones` WRITE;
/*!40000 ALTER TABLE `apelaciones` DISABLE KEYS */;
INSERT INTO `apelaciones` VALUES (1,2,'error pls','rechazada','2026-01-23 14:02:39'),(2,2,'prueba','rechazada','2026-01-23 14:05:18'),(3,2,'nooooo','aceptada','2026-01-23 14:13:32');
/*!40000 ALTER TABLE `apelaciones` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `audit_logs`
--

DROP TABLE IF EXISTS `audit_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `audit_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `usuario_responsable` varchar(50) DEFAULT NULL,
  `accion` varchar(50) DEFAULT NULL,
  `detalles` varchar(255) DEFAULT NULL,
  `fecha` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=80 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `audit_logs`
--

LOCK TABLES `audit_logs` WRITE;
/*!40000 ALTER TABLE `audit_logs` DISABLE KEYS */;
INSERT INTO `audit_logs` VALUES (1,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:45:55'),(2,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:50:04'),(3,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:50:06'),(4,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:10'),(5,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:10'),(6,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:10'),(7,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:10'),(8,'Gamer1','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:13'),(9,'Gamer1','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:13'),(10,'Gamer1','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:16'),(11,'Gamer1','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:16'),(12,'Gamer1','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:18'),(13,'Gamer1','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:18'),(14,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:34'),(15,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:35'),(16,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:35'),(17,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:50'),(18,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:51'),(19,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:51'),(20,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:51'),(21,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:51'),(22,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:51'),(23,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:51'),(24,'profe','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:51:52'),(25,'profe','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:51:54'),(26,'profe','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:52:49'),(27,'admin','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:54:49'),(28,'admin','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 12:54:51'),(29,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:12'),(30,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:14'),(31,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:14'),(32,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:23'),(33,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:24'),(34,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:24'),(35,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:25'),(36,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:26'),(37,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:26'),(38,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:26'),(39,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:26'),(40,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:55:26'),(41,'Gamer1','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:59:28'),(42,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 12:59:40'),(43,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:02:28'),(44,'admin','LOGIN_FAIL','Contraseña incorrecta','2026-01-23 13:02:37'),(45,'prueba','REGISTRO','Nuevo usuario creado','2026-01-23 13:02:53'),(46,'prueba','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:03:03'),(47,'prueba','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:03:34'),(48,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:03:44'),(49,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:41:48'),(50,'Admin','MODIFICAR_USUARIO','Editó al usuario ID 2','2026-01-23 13:42:34'),(51,'Admin','MODIFICAR_USUARIO','Editó al usuario ID 2','2026-01-23 13:42:53'),(52,'prueba','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:43:28'),(53,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:43:55'),(54,'Admin','MODIFICAR_USUARIO','Editó al usuario ID 2','2026-01-23 13:44:43'),(55,'Admin','MODIFICAR_USUARIO','Editó al usuario ID 2','2026-01-23 13:44:54'),(56,'Admin','MODIFICAR_USUARIO','Editó al usuario ID 2','2026-01-23 13:45:49'),(57,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 13:54:06'),(58,'Admin','MODIFICAR','Usuario ID 2: Estado: baneado->activo, Motivo: prueba jeje','2026-01-23 13:54:16'),(59,'Admin','MODIFICAR','Usuario ID 2: Rol: usuario->admin, Estado: activo->baneado (24 Horas), Motivo: prueba jeje','2026-01-23 13:54:44'),(60,'Admin','MODIFICAR','Usuario ID 2: Rol: admin->usuario, Tiempo baneo actualizado:  (1 Hora)','2026-01-23 13:55:16'),(61,'prueba','LOGIN_DENEGADO','Usuario baneado temporalmente intentó entrar','2026-01-23 13:55:33'),(62,'prueba','LOGIN_DENEGADO','Usuario baneado temporalmente intentó entrar','2026-01-23 14:02:30'),(63,'prueba','LOGIN_DENEGADO','Usuario baneado temporalmente intentó entrar','2026-01-23 14:02:58'),(64,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:03:23'),(65,'prueba','LOGIN_DENEGADO','Usuario baneado temporalmente intentó entrar','2026-01-23 14:05:12'),(66,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:05:24'),(67,'prueba','LOGIN_DENEGADO','Usuario baneado temporalmente intentó entrar','2026-01-23 14:05:59'),(68,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:06:53'),(69,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:12:02'),(70,'Admin','APELACION_RECHAZADA','Usuario: prueba. Decisión: APELACION_RECHAZADA','2026-01-23 14:12:25'),(71,'Admin','APELACION_RECHAZADA','Usuario: prueba. Decisión: APELACION_RECHAZADA','2026-01-23 14:12:29'),(72,'Admin','MODIFICAR','Usuario ID 2: Estado: baneado->activo','2026-01-23 14:12:41'),(73,'prueba','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:12:49'),(74,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:12:55'),(75,'Admin','MODIFICAR','Usuario ID 2: Estado: activo->baneado (1 Hora)','2026-01-23 14:13:03'),(76,'prueba','LOGIN_DENEGADO','Usuario baneado temporalmente intentó entrar','2026-01-23 14:13:26'),(77,'prueba','SOLICITUD_APELACION','El usuario ha solicitado revisión de su baneo.','2026-01-23 14:13:32'),(78,'admin','LOGIN_EXITO','Inicio de sesión correcto','2026-01-23 14:13:40'),(79,'Admin','APELACION_ACEPTADA','Usuario: prueba. Decisión: APELACION_ACEPTADA','2026-01-23 14:13:46');
/*!40000 ALTER TABLE `audit_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `usuarios`
--

DROP TABLE IF EXISTS `usuarios`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuarios` (
  `id` int NOT NULL AUTO_INCREMENT,
  `nombre_usuario` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `email` varchar(100) DEFAULT NULL,
  `fecha_registro` datetime DEFAULT CURRENT_TIMESTAMP,
  `estado_cuenta` enum('activo','baneado','suspendido') DEFAULT 'activo',
  `motivo_baneo` varchar(255) DEFAULT NULL,
  `fin_baneo` datetime DEFAULT NULL,
  `intentos_fallidos` int DEFAULT '0',
  `rol` varchar(20) NOT NULL DEFAULT 'usuario',
  PRIMARY KEY (`id`),
  UNIQUE KEY `nombre_usuario` (`nombre_usuario`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `usuarios`
--

LOCK TABLES `usuarios` WRITE;
/*!40000 ALTER TABLE `usuarios` DISABLE KEYS */;
INSERT INTO `usuarios` VALUES (1,'admin','03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4','admin@bitronix.com','2026-01-23 13:02:12','activo',NULL,NULL,0,'admin'),(2,'prueba','03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4','prueba@prueba.com','2026-01-23 13:02:53','activo',NULL,NULL,0,'usuario');
/*!40000 ALTER TABLE `usuarios` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-01-23 14:23:01
