-- DB-数据库设计-mysql.sql
-- 来源：D:\新建文件夹\DB-数据库设计.md
-- 目标数据库：MySQL 8.x
-- 默认数据库名：Test
-- 字符集建议：utf8mb4

CREATE DATABASE IF NOT EXISTS `Test`
DEFAULT CHARACTER SET utf8mb4
DEFAULT COLLATE utf8mb4_unicode_ci;

USE `Test`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- =========================
-- operators（操作员表）
-- 说明：按原设计保留“无主键约束”
-- =========================
CREATE TABLE IF NOT EXISTS `operators` (
    `userid`    VARCHAR(50)  NOT NULL,
    `username`  VARCHAR(100) NOT NULL,
    `pwd`       VARCHAR(255) NOT NULL,
    `usertype`  VARCHAR(50)  NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================
-- apparatus（设备表）
-- =========================
CREATE TABLE IF NOT EXISTS `apparatus` (
    `apparatusid`   INT NOT NULL,
    `innernumber`   VARCHAR(100) NOT NULL,
    `apparatusname` VARCHAR(100) NOT NULL,
    `checkdatef`    DATE NOT NULL,
    `checkdatet`    DATE NOT NULL,
    `pidport`       VARCHAR(50) NOT NULL,
    `powerport`     VARCHAR(50) NOT NULL,
    `constpower`    INT NULL,
    PRIMARY KEY (`apparatusid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================
-- productmaster（样品表）
-- =========================
CREATE TABLE IF NOT EXISTS `productmaster` (
    `productid`   VARCHAR(100) NOT NULL,
    `productname` VARCHAR(200) NOT NULL,
    `specific`    VARCHAR(200) NOT NULL,
    `diameter`    DOUBLE NOT NULL,
    `height`      DOUBLE NOT NULL,
    `flag`        VARCHAR(255) NULL,
    PRIMARY KEY (`productid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================
-- testmaster（试验记录表）
-- =========================
CREATE TABLE IF NOT EXISTS `testmaster` (
    `productid`        VARCHAR(100) NOT NULL,
    `testid`           VARCHAR(50) NOT NULL,
    `testdate`         DATE NOT NULL,
    `ambtemp`          DOUBLE NOT NULL,
    `ambhumi`          DOUBLE NOT NULL,
    `according`        VARCHAR(100) NOT NULL,
    `operator`         VARCHAR(100) NOT NULL,
    `apparatusid`      VARCHAR(50) NOT NULL,
    `apparatusname`    VARCHAR(100) NOT NULL,
    `apparatuschkdate` DATE NOT NULL,
    `rptno`            VARCHAR(100) NOT NULL,
    `preweight`        DOUBLE NOT NULL,
    `postweight`       DOUBLE NOT NULL,
    `lostweight`       DOUBLE NOT NULL,
    `lostweight_per`   DOUBLE NOT NULL,
    `totaltesttime`    INT NOT NULL,
    `constpower`       INT NOT NULL,
    `phenocode`        TEXT NOT NULL,
    `flametime`        INT NOT NULL,
    `flameduration`    INT NOT NULL,
    `maxtf1`           DOUBLE NOT NULL,
    `maxtf2`           DOUBLE NOT NULL,
    `maxts`            DOUBLE NOT NULL,
    `maxtc`            DOUBLE NOT NULL,
    `maxtf1_time`      INT NOT NULL,
    `maxtf2_time`      INT NOT NULL,
    `maxts_time`       INT NOT NULL,
    `maxtc_time`       INT NOT NULL,
    `finaltf1`         DOUBLE NOT NULL,
    `finaltf2`         DOUBLE NOT NULL,
    `finalts`          DOUBLE NOT NULL,
    `finaltc`          DOUBLE NOT NULL,
    `finaltf1_time`    INT NOT NULL,
    `finaltf2_time`    INT NOT NULL,
    `finalts_time`     INT NOT NULL,
    `finaltc_time`     INT NOT NULL,
    `deltatf1`         DOUBLE NOT NULL,
    `deltatf2`         DOUBLE NOT NULL,
    `deltatf`          DOUBLE NOT NULL,
    `deltats`          DOUBLE NOT NULL,
    `deltatc`          DOUBLE NOT NULL,
    `memo`             TEXT NULL,
    `flag`             VARCHAR(255) NULL,
    PRIMARY KEY (`productid`, `testid`),
    CONSTRAINT `FK_testmaster_productmaster`
        FOREIGN KEY (`productid`) REFERENCES `productmaster` (`productid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_Testmaster_Testdate` ON `testmaster` (`testdate`);
CREATE INDEX `IX_Testmaster_Operator` ON `testmaster` (`operator`);
CREATE INDEX `IX_Testmaster_Testdate_Productid` ON `testmaster` (`testdate`, `productid`);

-- =========================
-- sensors（传感器配置表）
-- =========================
CREATE TABLE IF NOT EXISTS `sensors` (
    `sensorid`    INT NOT NULL,
    `sensorname`  VARCHAR(100) NOT NULL,
    `dispname`    VARCHAR(100) NOT NULL,
    `sensorgroup` VARCHAR(100) NOT NULL,
    `unit`        VARCHAR(50) NOT NULL,
    `discription` VARCHAR(255) NOT NULL,
    `flag`        VARCHAR(50) NOT NULL,
    `signalzero`  DOUBLE NOT NULL,
    `signalspan`  DOUBLE NOT NULL,
    `outputzero`  DOUBLE NOT NULL,
    `outputspan`  DOUBLE NOT NULL,
    `outputvalue` DOUBLE NOT NULL,
    `inputvalue`  DOUBLE NOT NULL,
    `signaltype`  INT NOT NULL,
    PRIMARY KEY (`sensorid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================
-- CalibrationRecords（校准记录表）
-- 说明：TemperatureData / CenterTempData 按原设计保留为 JSON 字符串文本
-- =========================
CREATE TABLE IF NOT EXISTS `CalibrationRecords` (
    `Id`                 VARCHAR(36) NOT NULL,
    `CalibrationDate`    VARCHAR(50) NOT NULL,
    `CalibrationType`    VARCHAR(50) NOT NULL,
    `ApparatusId`        INT NOT NULL,
    `Operator`           VARCHAR(100) NOT NULL,
    `TemperatureData`    LONGTEXT NOT NULL,
    `UniformityResult`   DOUBLE NULL,
    `MaxDeviation`       DOUBLE NULL,
    `AverageTemperature` DOUBLE NULL,
    `PassedCriteria`     TINYINT(1) NOT NULL,
    `Remarks`            TEXT NOT NULL,
    `CreatedAt`          VARCHAR(50) NOT NULL,
    `TempA1` REAL NULL, `TempA2` REAL NULL, `TempA3` REAL NULL,
    `TempB1` REAL NULL, `TempB2` REAL NULL, `TempB3` REAL NULL,
    `TempC1` REAL NULL, `TempC2` REAL NULL, `TempC3` REAL NULL,
    `TAvg`        REAL NULL,
    `TAvgAxis1`   REAL NULL, `TAvgAxis2` REAL NULL, `TAvgAxis3` REAL NULL,
    `TAvgLevela`  REAL NULL, `TAvgLevelb` REAL NULL, `TAvgLevelc` REAL NULL,
    `TDevAxis1`   REAL NULL, `TDevAxis2` REAL NULL, `TDevAxis3` REAL NULL,
    `TDevLevela`  REAL NULL, `TDevLevelb` REAL NULL, `TDevLevelc` REAL NULL,
    `TAvgDevAxis` REAL NULL, `TAvgDevLevel` REAL NULL,
    `CenterTempData` LONGTEXT NULL,
    `Memo`           TEXT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_CalibrationRecord_Date` ON `CalibrationRecords` (`CalibrationDate`);
CREATE INDEX `IX_CalibrationRecord_Operator` ON `CalibrationRecords` (`Operator`);

-- =========================
-- 初始数据
-- =========================
INSERT INTO `operators` (`userid`, `username`, `pwd`, `usertype`)
SELECT '1', 'admin', '123456', 'admin'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `operators` WHERE `username` = 'admin'
);

INSERT INTO `operators` (`userid`, `username`, `pwd`, `usertype`)
SELECT '2', 'experimenter', '123456', 'operator'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `operators` WHERE `username` = 'experimenter'
);

INSERT INTO `apparatus` (`apparatusid`, `innernumber`, `apparatusname`, `checkdatef`, `checkdatet`, `pidport`, `powerport`, `constpower`)
SELECT 0, 'FURNACE-01', '一号试验炉', CURDATE(), DATE_ADD(CURDATE(), INTERVAL 1 YEAR), 'COM9', 'COM9', 2048
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `apparatus` WHERE `apparatusid` = 0
);

INSERT INTO `sensors` (`sensorid`, `sensorname`, `dispname`, `sensorgroup`, `unit`, `discription`, `flag`, `signalzero`, `signalspan`, `outputzero`, `outputspan`, `outputvalue`, `inputvalue`, `signaltype`)
SELECT 0,'Sensor0','炉温1','采集','℃','炉温1','启用',0,0,0,1000,0,0,4
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `sensors` WHERE `sensorid` = 0);

INSERT INTO `sensors` (`sensorid`, `sensorname`, `dispname`, `sensorgroup`, `unit`, `discription`, `flag`, `signalzero`, `signalspan`, `outputzero`, `outputspan`, `outputvalue`, `inputvalue`, `signaltype`)
SELECT 1,'Sensor1','炉温2','采集','℃','炉温2','启用',0,0,0,1000,0,0,4
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `sensors` WHERE `sensorid` = 1);

INSERT INTO `sensors` (`sensorid`, `sensorname`, `dispname`, `sensorgroup`, `unit`, `discription`, `flag`, `signalzero`, `signalspan`, `outputzero`, `outputspan`, `outputvalue`, `inputvalue`, `signaltype`)
SELECT 2,'Sensor2','表面温度','采集','℃','表面温度','启用',0,0,0,1000,0,0,4
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `sensors` WHERE `sensorid` = 2);

INSERT INTO `sensors` (`sensorid`, `sensorname`, `dispname`, `sensorgroup`, `unit`, `discription`, `flag`, `signalzero`, `signalspan`, `outputzero`, `outputspan`, `outputvalue`, `inputvalue`, `signaltype`)
SELECT 3,'Sensor3','中心温度','采集','℃','中心温度','启用',0,0,0,1000,0,0,4
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `sensors` WHERE `sensorid` = 3);

INSERT INTO `sensors` (`sensorid`, `sensorname`, `dispname`, `sensorgroup`, `unit`, `discription`, `flag`, `signalzero`, `signalspan`, `outputzero`, `outputspan`, `outputvalue`, `inputvalue`, `signaltype`)
SELECT 16,'Sensor16','校准温度','校准','℃','校准温度','启用',0,0,0,1000,0,0,4
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `sensors` WHERE `sensorid` = 16);

SET FOREIGN_KEY_CHECKS = 1;
