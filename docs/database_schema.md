# EUROERP - Database schema

> Generated from **LionEBDev** via `user-mssql_euroerp_dev` MCP on 2026-06-06.
> Use per story demand only. Re-export from the MCP when the schema changes.
>
> **Excluded:** `COMMISSION*`, `EMAIL` / `EMAIL_BATCH*`, and `MORTALITY` / `MORT_*` tables are not used in EUROERP and omitted from this document.

## Summary

- **Database:** LionEBDev
- **Tables:** 93 (COMMISSION_*, EMAIL*, MORTALITY/MORT_* excluded)
- **Views:** 9
- **Stored procedures:** 64
- **Foreign keys:** 102

## Table index

| Table | Columns |
|-------|---------|
| `ACTIVITY_ROLE` | 2 |
| `ANIMAL_SIZE` | 2 |
| `aspnet_Applications` | 4 |
| `aspnet_Membership` | 21 |
| `aspnet_Paths` | 4 |
| `aspnet_PersonalizationAllUsers` | 3 |
| `aspnet_PersonalizationPerUser` | 5 |
| `aspnet_Profile` | 5 |
| `aspnet_Roles` | 5 |
| `aspnet_SchemaVersions` | 3 |
| `aspnet_Users` | 7 |
| `aspnet_UsersInRoles` | 2 |
| `aspnet_WebEvent_Events` | 15 |
| `BANK` | 2 |
| `CALL` | 13 |
| `CALL_ORDER` | 2 |
| `CALL_STEP` | 8 |
| `CAR` | 9 |
| `CC_NFE` | 10 |
| `CFOP` | 6 |
| `CITY` | 4 |
| `CLIENT` | 55 |
| `CLIENT_CREDIT_HIST` | 8 |
| `CLIENT_DELIVERY_SUPPLIER_LINK` | 2 |
| `CLIENT_INV_SPECIAL` | 3 |
| `CLIENT_SALES_AGENTS_LINK` | 2 |
| `COUNTRY` | 4 |
| `CST` | 2 |
| `CST_CFOP` | 3 |
| `CST_CFOP_SN` | 3 |
| `CSTB` | 3 |
| `CURRENCY` | 5 |
| `CURRENCY_CONVERSION` | 3 |
| `DELIVERY_SUPPLIER_LINK` | 2 |
| `DISCOUNT` | 3 |
| `DISCOUNT_HIST` | 6 |
| `FINANCE_BILLS_TO_PAY` | 15 |
| `FINANCE_BILLS_TO_PAY_DETAIL` | 6 |
| `FINANCE_BTP_CHG_HST` | 9 |
| `FINANCE_BTR` | 10 |
| `FINANCE_BTR_CHG_HST` | 9 |
| `FINANCE_BTR_DETAIL` | 8 |
| `FINANCE_CHECK` | 8 |
| `FINANCE_PAYMENT` | 10 |
| `FINANCE_RECEIVE` | 12 |
| `FISCAL_CLASS` | 9 |
| `MARKET` | 3 |
| `MARKET_PRODUCT` | 6 |
| `MARKET_USER` | 2 |
| `ORDER` | 50 |
| `ORDER_DETAILS` | 15 |
| `ORDER_DETAILS_HIST` | 9 |
| `ORDER_EXPORT` | 6 |
| `ORDER_MODE` | 2 |
| `ORDER_PRODUCT_CLASS_LINK` | 2 |
| `ORDER_STATUS_HIST` | 5 |
| `PAYMENT_METHOD` | 5 |
| `PAYMENT_SUB_METHOD` | 5 |
| `PRODUCT` | 38 |
| `PRODUCT_CLASS` | 3 |
| `PRODUCT_GROUP` | 10 |
| `PRODUCT_SUPPLIER_LINK` | 2 |
| `PURCH_CONVERSION` | 3 |
| `PURCH_DETAILS` | 16 |
| `PURCH_HIST` | 3 |
| `PURCH_INDEX` | 2 |
| `PURCH_SP_CLIENT` | 5 |
| `PURCH_STOCK` | 5 |
| `PURCH_SUPPLIER` | 17 |
| `PURCHASE` | 5 |
| `RECEIPT` | 11 |
| `RECEIPT_CANCEL` | 7 |
| `RECEIPT_IN` | 10 |
| `RECEIPT_IN_DATA` | 46 |
| `RECEIPT_IN_DETAILS` | 8 |
| `RECEIPT_MSG` | 2 |
| `RETURN_ORDER` | 2 |
| `RETURN_ORDER_DETAILS` | 3 |
| `RETURN_ORDER_HIST` | 7 |
| `RETURNING` | 9 |
| `SEC_ACTIVITY` | 3 |
| `SEC_ACTV_RULES` | 3 |
| `STATE` | 7 |
| `STOCK_HISTORY` | 7 |
| `STOCK_IN` | 25 |
| `STOCK_IN_DETAIL` | 14 |
| `SUPPLIER` | 42 |
| `SUPPLIER_GROUP` | 3 |
| `SYS_CONTROL` | 2 |
| `sysdiagrams` | 5 |
| `UNITS` | 5 |
| `WARRANTY` | 21 |
| `ZONE` | 2 |

## Tables

### `ACTIVITY_ROLE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ACTV_ID` | smallint(5) | NO |  |
| 2 | `ROLE_ID` | uniqueidentifier | NO |  |

### `ANIMAL_SIZE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(20) | NO |  |

### `aspnet_Applications`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ApplicationName` | nvarchar(256) | NO |  |
| 2 | `LoweredApplicationName` | nvarchar(256) | NO |  |
| 3 | `ApplicationId` | uniqueidentifier | NO | (newid()) |
| 4 | `Description` | nvarchar(256) | YES |  |

### `aspnet_Membership`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ApplicationId` | uniqueidentifier | NO |  |
| 2 | `UserId` | uniqueidentifier | NO |  |
| 3 | `Password` | nvarchar(128) | NO |  |
| 4 | `PasswordFormat` | int(10) | NO | ((0)) |
| 5 | `PasswordSalt` | nvarchar(128) | NO |  |
| 6 | `MobilePIN` | nvarchar(16) | YES |  |
| 7 | `Email` | nvarchar(256) | YES |  |
| 8 | `LoweredEmail` | nvarchar(256) | YES |  |
| 9 | `PasswordQuestion` | nvarchar(256) | YES |  |
| 10 | `PasswordAnswer` | nvarchar(128) | YES |  |
| 11 | `IsApproved` | bit | NO |  |
| 12 | `IsLockedOut` | bit | NO |  |
| 13 | `CreateDate` | datetime | NO |  |
| 14 | `LastLoginDate` | datetime | NO |  |
| 15 | `LastPasswordChangedDate` | datetime | NO |  |
| 16 | `LastLockoutDate` | datetime | NO |  |
| 17 | `FailedPasswordAttemptCount` | int(10) | NO |  |
| 18 | `FailedPasswordAttemptWindowStart` | datetime | NO |  |
| 19 | `FailedPasswordAnswerAttemptCount` | int(10) | NO |  |
| 20 | `FailedPasswordAnswerAttemptWindowStart` | datetime | NO |  |
| 21 | `Comment` | ntext(1073741823) | YES |  |

### `aspnet_Paths`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ApplicationId` | uniqueidentifier | NO |  |
| 2 | `PathId` | uniqueidentifier | NO | (newid()) |
| 3 | `Path` | nvarchar(256) | NO |  |
| 4 | `LoweredPath` | nvarchar(256) | NO |  |

### `aspnet_PersonalizationAllUsers`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PathId` | uniqueidentifier | NO |  |
| 2 | `PageSettings` | image(2147483647) | NO |  |
| 3 | `LastUpdatedDate` | datetime | NO |  |

### `aspnet_PersonalizationPerUser`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `Id` | uniqueidentifier | NO | (newid()) |
| 2 | `PathId` | uniqueidentifier | YES |  |
| 3 | `UserId` | uniqueidentifier | YES |  |
| 4 | `PageSettings` | image(2147483647) | NO |  |
| 5 | `LastUpdatedDate` | datetime | NO |  |

### `aspnet_Profile`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `UserId` | uniqueidentifier | NO |  |
| 2 | `PropertyNames` | ntext(1073741823) | NO |  |
| 3 | `PropertyValuesString` | ntext(1073741823) | NO |  |
| 4 | `PropertyValuesBinary` | image(2147483647) | NO |  |
| 5 | `LastUpdatedDate` | datetime | NO |  |

### `aspnet_Roles`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ApplicationId` | uniqueidentifier | NO |  |
| 2 | `RoleId` | uniqueidentifier | NO | (newid()) |
| 3 | `RoleName` | nvarchar(256) | NO |  |
| 4 | `LoweredRoleName` | nvarchar(256) | NO |  |
| 5 | `Description` | nvarchar(256) | YES |  |

### `aspnet_SchemaVersions`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `Feature` | nvarchar(128) | NO |  |
| 2 | `CompatibleSchemaVersion` | nvarchar(128) | NO |  |
| 3 | `IsCurrentVersion` | bit | NO |  |

### `aspnet_Users`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ApplicationId` | uniqueidentifier | NO |  |
| 2 | `UserId` | uniqueidentifier | NO | (newid()) |
| 3 | `UserName` | nvarchar(256) | NO |  |
| 4 | `LoweredUserName` | nvarchar(256) | NO |  |
| 5 | `MobileAlias` | nvarchar(16) | YES | (NULL) |
| 6 | `IsAnonymous` | bit | NO | ((0)) |
| 7 | `LastActivityDate` | datetime | NO |  |

### `aspnet_UsersInRoles`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `UserId` | uniqueidentifier | NO |  |
| 2 | `RoleId` | uniqueidentifier | NO |  |

### `aspnet_WebEvent_Events`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `EventId` | char(32) | NO |  |
| 2 | `EventTimeUtc` | datetime | NO |  |
| 3 | `EventTime` | datetime | NO |  |
| 4 | `EventType` | nvarchar(256) | NO |  |
| 5 | `EventSequence` | decimal(19,0) | NO |  |
| 6 | `EventOccurrence` | decimal(19,0) | NO |  |
| 7 | `EventCode` | int(10) | NO |  |
| 8 | `EventDetailCode` | int(10) | NO |  |
| 9 | `Message` | nvarchar(1024) | YES |  |
| 10 | `ApplicationPath` | nvarchar(256) | YES |  |
| 11 | `ApplicationVirtualPath` | nvarchar(256) | YES |  |
| 12 | `MachineName` | nvarchar(256) | NO |  |
| 13 | `RequestUrl` | nvarchar(1024) | YES |  |
| 14 | `ExceptionType` | nvarchar(256) | YES |  |
| 15 | `Details` | ntext(1073741823) | YES |  |

### `BANK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(30) | NO |  |

### `CALL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `TYPE` | varchar(1) | NO |  |
| 3 | `CLIENT_ID` | int(10) | YES |  |
| 4 | `SUPPLIER_ID` | int(10) | YES |  |
| 5 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 6 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |
| 7 | `APPLICATION_ID` | varchar(8) | NO |  |
| 8 | `USER_ID` | varchar(20) | NO |  |
| 9 | `EVENT_DATE` | smalldatetime | NO |  |
| 10 | `DUE_DATE` | smalldatetime | NO |  |
| 11 | `STATUS` | varchar(1) | NO |  |
| 12 | `ZONE_ID` | int(10) | YES |  |
| 13 | `DELIVERY_ID` | int(10) | YES |  |

### `CALL_ORDER`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CALL_ID` | int(10) | NO |  |
| 2 | `ORDER_ID` | int(10) | NO |  |

### `CALL_STEP`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CALL_ID` | int(10) | NO |  |
| 2 | `STEP_NO` | int(10) | NO |  |
| 3 | `DESCRIPTION` | varchar(300) | NO |  |
| 4 | `STATUS` | varchar(1) | NO |  |
| 5 | `RESPONSIBLE_USER` | uniqueidentifier | NO |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 8 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |

### `CAR`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `DESCRIPTION` | varchar(150) | NO |  |
| 3 | `PLATE` | varchar(150) | NO |  |
| 4 | `LAST_KM` | bigint(19) | NO |  |
| 5 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 6 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |
| 7 | `USER_ID` | varchar(20) | NO |  |
| 8 | `APPLICATION_ID` | varchar(20) | NO |  |
| 9 | `CLIENT_ID` | int(10) | NO |  |

### `CC_NFE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `RECEIPT_NO` | int(10) | NO |  |
| 2 | `ORDER_ID` | int(10) | NO |  |
| 3 | `REASON` | varchar(1000) | NO |  |
| 4 | `SEQ_NO` | tinyint(3) | NO |  |
| 5 | `NFE_STATUS` | varchar(3) | YES |  |
| 6 | `EMAIL_COUNT` | int(10) | YES |  |
| 7 | `USER_ID` | varchar(20) | NO |  |
| 8 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 9 | `APPLICATION_ID` | varchar(8) | NO |  |
| 10 | `PROTOCOL` | varchar(15) | YES |  |

### `CFOP`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `CODE` | varchar(50) | NO |  |
| 3 | `DESCRIPTION` | varchar(50) | NO |  |
| 4 | `LABEL` | varchar(15) | YES |  |
| 5 | `ICMS_IND` | tinyint(3) | YES |  |
| 6 | `LAYOUT_ID` | varchar(3) | NO |  |

### `CITY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | smallint(5) | NO |  |
| 2 | `NAME` | varchar(30) | NO |  |
| 3 | `STATE_ID` | tinyint(3) | NO |  |
| 4 | `C_MUN` | varchar(7) | YES |  |

### `CLIENT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `CNPJPF` | varchar(20) | NO |  |
| 3 | `PERSON_TYPE` | varchar(1) | NO |  |
| 4 | `FANTASY_NAME` | varchar(100) | YES |  |
| 5 | `SOCIAL_NAME` | varchar(100) | NO |  |
| 6 | `ADDRESS_STREET` | varchar(150) | YES |  |
| 7 | `ADDRESS_BLOCK` | varchar(100) | YES |  |
| 8 | `ADDRESS_NUMBER` | varchar(100) | YES |  |
| 9 | `ADDRESS_STATE_ID` | tinyint(3) | NO |  |
| 10 | `ADDRESS_CITY_ID` | smallint(5) | NO |  |
| 11 | `ADDRESS_ZIPCODE` | varchar(9) | YES |  |
| 12 | `PHONE1` | varchar(60) | YES |  |
| 13 | `PHONE2` | varchar(60) | YES |  |
| 14 | `PHONE3` | varchar(60) | YES |  |
| 15 | `FAX_NO` | varchar(60) | YES |  |
| 16 | `CELULAR` | varchar(60) | YES |  |
| 17 | `STATE_INSCR` | varchar(30) | YES | ('ISENTO') |
| 18 | `OBS` | varchar(500) | YES |  |
| 19 | `CONTACT` | varchar(100) | YES |  |
| 20 | `PAYMENT_METHOD_ID` | tinyint(3) | YES |  |
| 21 | `EMAIL` | varchar(200) | YES |  |
| 22 | `BIRTHDAY` | tinyint(3) | YES |  |
| 23 | `AVG_PAYTERM` | tinyint(3) | YES |  |
| 24 | `LIMIT_AMOUNT` | money(19) | YES |  |
| 25 | `BILL_ADDRESS_STREET` | varchar(150) | YES |  |
| 26 | `BILL_ADDRESS_BLOCK` | varchar(100) | YES |  |
| 27 | `BILL_ADDRESS_NUMBER` | smallint(5) | YES |  |
| 28 | `BILL_ADDRESS_ZIPCODE` | varchar(9) | YES |  |
| 29 | `BILL_ADDRESS_INDICATOR` | varchar(1) | NO |  |
| 30 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 31 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 32 | `APPLICATION_ID` | varchar(8) | NO |  |
| 33 | `USER_ID` | varchar(20) | NO |  |
| 34 | `LEDGE` | varchar(1) | NO |  |
| 35 | `BIRTHMONTH` | tinyint(3) | YES |  |
| 36 | `ACTIVE` | varchar(1) | NO | ('Y') |
| 37 | `ADDRESS_COMPLEMENT` | varchar(200) | YES |  |
| 38 | `PAYMENT_METHOD_ID2` | tinyint(3) | YES |  |
| 39 | `CREDIT` | money(19) | YES |  |
| 40 | `ALLOW_DELINQ` | datetime | YES |  |
| 41 | `COUNT_FOR_ORDERING` | varchar(1) | YES |  |
| 42 | `MARKET_ID` | tinyint(3) | NO | ((1)) |
| 43 | `DRE` | varchar(1) | YES |  |
| 44 | `SPECIAL` | varchar(1) | YES |  |
| 45 | `COMMISSION` | bit | NO | ((1)) |
| 46 | `ADDRESS_COUNTRY_ID` | int(10) | NO | ((1)) |
| 47 | `IGNORE_DELINQ` | bit | NO | ((0)) |
| 48 | `ALLOW_DELINQ_USER` | varchar(20) | YES |  |
| 49 | `PAYMENT_METHOD_ID3` | tinyint(3) | YES |  |
| 50 | `PAYMENT_METHOD_ID4` | tinyint(3) | YES |  |
| 51 | `PAYMENT_METHOD_ID5` | tinyint(3) | YES |  |
| 52 | `COST_IND` | bit | NO | ((0)) |
| 53 | `PASSWORD` | varchar(6) | YES |  |
| 54 | `EMAIL_ACTIVE` | nchar(1) | NO | ('Y') |
| 55 | `MUN_INSCR` | varchar(30) | YES |  |

### `CLIENT_CREDIT_HIST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | datetime | NO |  |
| 2 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `USER_ID` | varchar(20) | NO |  |
| 5 | `AMOUNT` | money(19) | NO |  |
| 6 | `CLIENT_ID` | int(10) | NO |  |
| 7 | `ORDER_ID` | int(10) | YES |  |
| 8 | `MEMO` | varchar(500) | YES |  |

### `CLIENT_DELIVERY_SUPPLIER_LINK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CLIENT_ID` | int(10) | NO |  |
| 2 | `DELIVERY_SUPPLIER_ID` | int(10) | NO |  |

### `CLIENT_INV_SPECIAL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CLIENT_ID` | int(10) | NO |  |
| 2 | `LEDGE` | varchar(1) | NO |  |
| 3 | `PKId` | tinyint(3) | NO |  |

### `CLIENT_SALES_AGENTS_LINK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CLIENT_ID` | int(10) | NO |  |
| 2 | `USER_ID` | uniqueidentifier | NO |  |

### `COUNTRY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `NAME` | varchar(50) | NO |  |
| 2 | `PKId` | int(10) | NO |  |
| 3 | `CODE` | varchar(5) | YES |  |
| 4 | `IPI` | decimal(4,2) | YES |  |

### `CST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(50) | NO |  |

### `CST_CFOP`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CFOP_ID` | tinyint(3) | NO |  |
| 2 | `STATE_ID` | tinyint(3) | NO |  |
| 3 | `CSTB_ID` | varchar(3) | NO |  |

### `CST_CFOP_SN`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CFOP_ID` | tinyint(3) | NO |  |
| 2 | `STATE_ID` | tinyint(3) | NO |  |
| 3 | `CSTB_ID` | varchar(3) | NO |  |

### `CSTB`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | varchar(3) | NO |  |
| 2 | `NAME` | varchar(50) | NO |  |
| 3 | `ICMS_IND` | tinyint(3) | YES |  |

### `CURRENCY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(15) | NO |  |
| 3 | `CODE` | varchar(3) | NO |  |
| 4 | `SYMBOL` | nvarchar(5) | NO |  |
| 5 | `DEFAULT_CURR` | tinyint(3) | YES |  |

### `CURRENCY_CONVERSION`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SOURCE_CURRENCY_ID` | tinyint(3) | NO |  |
| 2 | `TARGET_CURRENCY_ID` | tinyint(3) | NO |  |
| 3 | `CONVERSION` | decimal(4,3) | NO |  |

### `DELIVERY_SUPPLIER_LINK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SUPPLIER_ID` | int(10) | NO |  |
| 2 | `DELIVERY_SUPPLIER_ID` | int(10) | NO |  |

### `DISCOUNT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CLIENT_ID` | int(10) | NO |  |
| 2 | `PRODUCT_GROUP_ID` | tinyint(3) | NO |  |
| 3 | `DISCOUNT` | decimal(4,2) | YES |  |

### `DISCOUNT_HIST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 2 | `APPLICATION_ID` | varchar(8) | NO |  |
| 3 | `USER_ID` | varchar(20) | NO |  |
| 4 | `ORDER_ID` | int(10) | NO |  |
| 5 | `AMOUNT` | tinyint(3) | NO |  |
| 6 | `MEMO` | varchar(300) | NO |  |

### `FINANCE_BILLS_TO_PAY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SUPPLIER_ID` | int(10) | NO |  |
| 3 | `SYS_CREATION_DATE` | datetime | NO |  |
| 4 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 5 | `APPLICATION_ID` | varchar(8) | NO |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `PAYMENT_METHOD_ID` | tinyint(3) | NO |  |
| 8 | `TERMS` | tinyint(3) | NO |  |
| 9 | `BILL_TYPE` | varchar(1) | NO |  |
| 10 | `STOCK_IN_ID` | int(10) | YES |  |
| 11 | `MANUAL_BILL_ID` | int(10) | YES |  |
| 12 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 13 | `ORDER_DATE` | datetime | YES |  |
| 14 | `CONVERSION` | decimal(4,2) | YES |  |
| 15 | `PURCH_ID` | int(10) | YES |  |

### `FINANCE_BILLS_TO_PAY_DETAIL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `FINANCE_BILL_ID` | int(10) | NO |  |
| 2 | `DUE_DATE` | datetime | NO |  |
| 3 | `AMOUNT` | money(19) | NO |  |
| 4 | `STATUS` | varchar(1) | NO |  |
| 5 | `TERM_NO` | tinyint(3) | NO |  |
| 6 | `MEMO` | varchar(200) | YES |  |

### `FINANCE_BTP_CHG_HST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | datetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `FINANCE_BILL_ID` | int(10) | NO |  |
| 5 | `TERM_NO` | tinyint(3) | NO |  |
| 6 | `AMOUNT` | money(19) | NO |  |
| 7 | `STATUS` | varchar(1) | NO |  |
| 8 | `MEMO` | varchar(200) | YES |  |
| 9 | `DUE_DATE` | datetime | NO |  |

### `FINANCE_BTR`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SYS_CREATION_DATE` | datetime | NO |  |
| 3 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 4 | `USER_ID` | varchar(20) | NO |  |
| 5 | `APPLICATION_ID` | varchar(8) | NO |  |
| 6 | `CLIENT_ID` | int(10) | NO |  |
| 7 | `PAYMENT_METHOD_ID_` | tinyint(3) | YES |  |
| 8 | `TERMS` | tinyint(3) | NO |  |
| 9 | `BILL_TYPE` | varchar(1) | NO |  |
| 10 | `CURRENCY_ID` | tinyint(3) | NO |  |

### `FINANCE_BTR_CHG_HST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | datetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `FINANCE_BTR_ID` | int(10) | NO |  |
| 5 | `TERM_NO` | smallint(5) | NO |  |
| 6 | `AMOUNT` | money(19) | NO |  |
| 7 | `STATUS` | varchar(1) | NO |  |
| 8 | `MEMO` | varchar(200) | YES |  |
| 9 | `DUE_DATE` | datetime | NO |  |

### `FINANCE_BTR_DETAIL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `FINANCE_BTR_ID` | int(10) | NO |  |
| 2 | `TERM_NO` | tinyint(3) | NO |  |
| 3 | `DUE_DATE` | datetime | NO |  |
| 4 | `AMOUNT` | money(19) | NO |  |
| 5 | `STATUS` | varchar(1) | NO |  |
| 6 | `MEMO` | varchar(200) | YES |  |
| 7 | `PAYMENT_METHOD_ID` | tinyint(3) | NO |  |
| 8 | `PAYMENT_SUB_METHOD_ID` | tinyint(3) | YES |  |

### `FINANCE_CHECK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `FINANCE_BTR_ID` | int(10) | NO |  |
| 2 | `TERM_NO` | tinyint(3) | NO |  |
| 3 | `PKId` | int(10) | NO |  |
| 4 | `NUMBER` | varchar(15) | NO |  |
| 5 | `BANK_NO` | varchar(15) | NO |  |
| 6 | `AGENCY_NO` | varchar(15) | NO |  |
| 7 | `AMOUNT` | money(19) | NO |  |
| 8 | `CHECK_NAME` | varchar(50) | NO |  |

### `FINANCE_PAYMENT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `FINANCE_BILL_ID` | int(10) | NO |  |
| 2 | `TERM_NO` | tinyint(3) | NO |  |
| 3 | `PKId` | int(10) | NO |  |
| 4 | `SYS_CREATION_DATE` | datetime | NO |  |
| 5 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `APPLICATION_ID` | varchar(8) | NO |  |
| 8 | `AMOUNT` | money(19) | NO |  |
| 9 | `MEMO` | varchar(200) | YES |  |
| 10 | `PAYMENT_DATE` | datetime | YES |  |

### `FINANCE_RECEIVE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `FINANCE_BTR_ID` | int(10) | NO |  |
| 2 | `TERM_NO` | tinyint(3) | NO |  |
| 3 | `PKId` | int(10) | NO |  |
| 4 | `SYS_CREATION_DATE` | datetime | NO |  |
| 5 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `APPLICATION_ID` | varchar(8) | NO |  |
| 8 | `AMOUNT` | money(19) | NO |  |
| 9 | `MEMO` | varchar(200) | YES |  |
| 10 | `COMMISSION_ID` | int(10) | YES |  |
| 11 | `TYPE` | varchar(1) | NO | ('M') |
| 12 | `RETURN_ID` | int(10) | NO | ((0)) |

### `FISCAL_CLASS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `NAME` | varchar(50) | NO |  |
| 3 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 4 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 5 | `APPLICATION_ID` | varchar(8) | NO |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `VALUE` | varchar(20) | YES |  |
| 8 | `IPI` | decimal(4,2) | NO | ((0)) |
| 9 | `ICMSST` | bit | YES | ((0)) |

### `MARKET`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(20) | NO |  |
| 3 | `CURRENCY_ID` | tinyint(3) | NO |  |

### `MARKET_PRODUCT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PRODUCT_ID` | int(10) | NO |  |
| 2 | `MARKET_ID` | tinyint(3) | NO |  |
| 3 | `NAME` | varchar(200) | YES |  |
| 4 | `SCI_NAME` | varchar(150) | YES |  |
| 5 | `PROFIT` | decimal(7,3) | NO | ((0)) |
| 6 | `PRICE` | money(19) | NO | ((0)) |

### `MARKET_USER`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `USER_ID` | uniqueidentifier | NO |  |
| 2 | `MARKET_ID` | tinyint(3) | NO |  |

### `ORDER`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SYS_CREATION_DATE` | datetime | NO |  |
| 3 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 4 | `APPLICATION_ID` | varchar(8) | NO |  |
| 5 | `USER_ID` | varchar(20) | NO |  |
| 6 | `LAST_ACTV` | varchar(8) | NO |  |
| 7 | `CLIENT_ID` | int(10) | NO |  |
| 8 | `STATUS` | varchar(1) | NO |  |
| 9 | `SHIPMENT_COST` | money(19) | YES |  |
| 10 | `STATUS_CHG_DATE` | datetime | YES |  |
| 11 | `BTR_ID` | int(10) | YES |  |
| 12 | `SALES_AGENT` | varchar(20) | NO |  |
| 13 | `ORDER_TYPE` | varchar(1) | NO |  |
| 14 | `DISCOUNT` | decimal(4,2) | YES |  |
| 15 | `CREDIT` | money(19) | YES |  |
| 16 | `MEMO` | varchar(300) | YES |  |
| 17 | `SENT_DATE` | datetime | YES |  |
| 18 | `RECEIPT` | int(10) | YES |  |
| 19 | `RECEIPT_AMOUNT101` | money(19) | YES |  |
| 20 | `RECEIPT_ICMS` | tinyint(3) | YES |  |
| 21 | `AVG_COMMISSION` | decimal(6,4) | YES |  |
| 22 | `AVG_SUP_COMMISSION` | decimal(6,4) | YES |  |
| 23 | `AMT_SIGNAL` | tinyint(3) | NO | ((1)) |
| 24 | `RET_REF_ORDER_ID` | int(10) | YES |  |
| 25 | `RECEIPT_AMOUNT102` | money(19) | YES |  |
| 26 | `RECEIPT_AMOUNT500` | money(19) | YES |  |
| 27 | `NFE_RECEIPT` | varchar(100) | YES |  |
| 28 | `NFE_STATUS` | char(1) | YES |  |
| 29 | `NFE_KEY` | varchar(50) | YES |  |
| 30 | `NFE_PROTOCOL` | varchar(17) | YES |  |
| 31 | `NFE_CANCEL_PROTOCOL` | varchar(15) | YES |  |
| 32 | `NFE_EMAIL_COUNT` | tinyint(3) | YES |  |
| 33 | `DELIVERY_SUPPLIER_ID` | int(10) | YES |  |
| 34 | `NFE_PROTOCOL_RESULT` | varchar(4) | YES |  |
| 35 | `CAR_ID` | int(10) | YES |  |
| 36 | `CAR_KM` | bigint(19) | YES |  |
| 37 | `CAR_PROBLEM` | varchar(MAX) | YES |  |
| 38 | `RECEIPT_AMOUNT103` | money(19) | YES |  |
| 39 | `RECEIPT_AMOUNT202` | money(19) | YES |  |
| 40 | `RECEIPT_AMOUNT900` | money(19) | YES |  |
| 41 | `RECEIPT_AMOUNT500_vST` | money(19) | YES |  |
| 42 | `OTHER_EXPENSES` | money(19) | NO | ((0)) |
| 43 | `CHARGE_SHIPMENT` | bit | NO | ((0)) |
| 44 | `RPS_NO` | varchar(15) | YES |  |
| 45 | `NFES_NO` | varchar(15) | YES |  |
| 46 | `NFES_CHECK_CODE` | varchar(20) | YES |  |
| 47 | `NFES_EMAIL_COUNT` | tinyint(3) | YES |  |
| 48 | `PAYMENT_SUB_METHOD_ID` | tinyint(3) | YES |  |
| 49 | `MODE` | varchar(1) | NO | ('S') |
| 50 | `HIGI_PROC` | varchar(1) | NO | ('N') |

### `ORDER_DETAILS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ORDER_ID` | int(10) | NO |  |
| 2 | `PRODUCT_ID` | int(10) | NO |  |
| 3 | `QUANTITY` | decimal(9,3) | NO |  |
| 4 | `PRICE` | money(19) | NO |  |
| 5 | `DISCOUNT` | decimal(4,2) | NO |  |
| 6 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 7 | `CONVERSION` | decimal(5,3) | NO |  |
| 8 | `COST_FINAL` | money(19) | YES |  |
| 9 | `IGNORE_ORDER_DISC` | bit | YES |  |
| 10 | `CHG_MARK` | bit | YES |  |
| 11 | `QTD_ORDERED` | decimal(9,3) | NO | ((0)) |
| 12 | `BOX` | tinyint(3) | NO | ((0)) |
| 13 | `UNIT_ID` | int(10) | NO | ((1)) |
| 14 | `WORKMAN` | varchar(20) | YES |  |
| 15 | `HAS_COST_IND` | bit | NO |  |

### `ORDER_DETAILS_HIST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ORDER_ID` | int(10) | NO |  |
| 2 | `PRODUCT_ID` | int(10) | NO |  |
| 3 | `LAST_QTD` | smallint(5) | NO |  |
| 4 | `NEW_QTD` | smallint(5) | NO |  |
| 5 | `SYS_CREATION_DATE` | datetime | NO |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `APPLICATION_ID` | varchar(8) | NO |  |
| 8 | `MEMO` | varchar(300) | YES |  |
| 9 | `QTD_ORDERED` | smallint(5) | NO |  |

### `ORDER_EXPORT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ORDER_ID` | int(10) | NO |  |
| 2 | `SHIPPED_VIA` | varchar(100) | YES |  |
| 3 | `AGENT_FEE` | money(19) | YES |  |
| 4 | `HANDLING_BROKER` | money(19) | YES |  |
| 5 | `HEALTH_FEE` | money(19) | YES |  |
| 6 | `BOX_CHARGE` | money(19) | YES |  |

### `ORDER_MODE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ORDER_MODE` | varchar(1) | NO |  |
| 2 | `DESCRIPTION` | varchar(50) | NO |  |

### `ORDER_PRODUCT_CLASS_LINK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ORDER_TYPE` | varchar(1) | NO |  |
| 2 | `PRODUCT_CLASS_ID` | tinyint(3) | NO |  |

### `ORDER_STATUS_HIST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | datetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `NEW_STATUS` | varchar(1) | NO |  |
| 5 | `ORDER_ID` | int(10) | NO |  |

### `PAYMENT_METHOD`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `NAME` | varchar(50) | NO |  |
| 2 | `PKId` | tinyint(3) | NO |  |
| 3 | `MAX_TERMS` | tinyint(3) | YES |  |
| 4 | `MIN_AMOUNT` | money(19) | YES |  |
| 5 | `LABEL` | varchar(50) | YES |  |

### `PAYMENT_SUB_METHOD`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(50) | NO |  |
| 3 | `PAYMENT_METHOD_ID` | tinyint(3) | NO |  |
| 4 | `MAX_TERMS` | tinyint(3) | YES |  |
| 5 | `MIN_AMOUNT` | money(19) | YES |  |

### `PRODUCT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `GROUP_ID` | tinyint(3) | NO |  |
| 3 | `NAME` | varchar(200) | NO |  |
| 4 | `COST_GROSS` | money(19) | NO |  |
| 5 | `COST_TRANSPORT` | decimal(4,2) | NO |  |
| 6 | `PROFIT_` | decimal(5,2) | YES |  |
| 7 | `COST_FINAL` | money(19) | NO |  |
| 8 | `PRICE_` | money(19) | YES |  |
| 9 | `WEIGHT` | int(10) | YES |  |
| 10 | `FISCAL_CLASS_ID` | int(10) | YES |  |
| 11 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 12 | `CST_ID` | tinyint(3) | NO |  |
| 13 | `pH` | decimal(3,1) | YES |  |
| 14 | `BAR_CODE` | varchar(15) | YES |  |
| 15 | `STOCK` | decimal(9,3) | NO |  |
| 16 | `STOCK_MIN` | smallint(5) | NO |  |
| 17 | `STOCK_LAST_IN_DATE` | smalldatetime | YES |  |
| 18 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 19 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 20 | `APPLICATION_ID` | varchar(8) | NO |  |
| 21 | `USER_ID` | varchar(20) | NO |  |
| 22 | `SIZE_ID` | tinyint(3) | YES |  |
| 23 | `DISCOUNT` | decimal(4,2) | YES |  |
| 24 | `COST_NET` | money(19) | NO |  |
| 25 | `EXTERNAL_PKID` | varchar(20) | YES |  |
| 26 | `ACTIVE` | varchar(1) | NO | ('Y') |
| 27 | `DESCRIPTION` | varchar(300) | YES |  |
| 28 | `IGNORE_ORDER_DISC` | bit | YES |  |
| 29 | `AVG_COST_FINAL` | money(19) | NO | ((0)) |
| 30 | `NAME_IBAMA` | varchar(200) | YES |  |
| 31 | `SCI_NAME_IBAMA` | varchar(150) | YES |  |
| 32 | `QUARANTINE` | bit | YES |  |
| 33 | `PACK` | int(10) | NO | ((1)) |
| 34 | `CSTB_ID` | varchar(3) | NO | ('00') |
| 35 | `CFAT` | bit | YES |  |
| 36 | `UNIT_ID` | int(10) | NO | ((1)) |
| 37 | `HAS_COST_IND` | bit | NO |  |
| 38 | `IPI` | decimal(4,2) | NO | ((0)) |

### `PRODUCT_CLASS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(50) | NO |  |
| 3 | `PROD_SRV_IND` | char(1) | NO |  |

### `PRODUCT_GROUP`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(150) | NO |  |
| 3 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 4 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 5 | `APPLICATION_ID` | varchar(8) | NO |  |
| 6 | `USER_ID` | varchar(20) | NO |  |
| 7 | `PRODUCT_CLASS_ID` | tinyint(3) | NO |  |
| 8 | `IGNORE_ORDER_DISC` | bit | YES |  |
| 9 | `BRAND` | bit | NO | ((0)) |
| 10 | `BRAND_COMMISSION` | bit | NO | ((0)) |

### `PRODUCT_SUPPLIER_LINK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PRODUCT_ID` | int(10) | NO |  |
| 2 | `SUPPLIER_ID` | int(10) | NO |  |

### `PURCH_CONVERSION`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PURCH_ID` | int(10) | NO |  |
| 2 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 3 | `CONVERSION` | decimal(4,2) | NO |  |

### `PURCH_DETAILS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PURCH_ID` | int(10) | NO |  |
| 2 | `SUPPLIER_ID` | int(10) | NO |  |
| 3 | `PRODUCT_ID` | int(10) | NO |  |
| 4 | `SOLD` | int(10) | NO |  |
| 5 | `STOCK` | int(10) | NO |  |
| 6 | `UNIT_PRICE` | money(19) | NO |  |
| 7 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 8 | `STOCK_MIN` | int(10) | NO |  |
| 9 | `PROJ` | decimal(9,3) | NO |  |
| 10 | `ORDER` | int(10) | NO |  |
| 11 | `DISCOUNT` | decimal(4,2) | NO | ((0)) |
| 12 | `RECEIVED` | decimal(9,3) | NO | ((0)) |
| 13 | `PREVIOUS` | decimal(9,3) | NO | ((0)) |
| 14 | `ORDERED` | int(10) | NO | ((0)) |
| 15 | `PACK` | int(10) | NO | ((1)) |
| 16 | `NEW_PRICE` | money(19) | YES | ((0)) |

### `PURCH_HIST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `LOG` | varchar(100) | NO |  |

### `PURCH_INDEX`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `DAYS` | int(10) | NO |  |
| 2 | `ORDER_ID` | int(10) | NO |  |

### `PURCH_SP_CLIENT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PURCH_ID` | int(10) | NO |  |
| 2 | `SUPPLIER_ID` | int(10) | NO |  |
| 3 | `PRODUCT_ID` | int(10) | NO |  |
| 4 | `CLIENT_ID` | int(10) | NO |  |
| 5 | `QUANTITY` | int(10) | NO | ((0)) |

### `PURCH_STOCK`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `SUPPLIER_ID` | int(10) | NO |  |
| 5 | `PURCH_ID` | int(10) | NO |  |

### `PURCH_SUPPLIER`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PURCH_ID` | int(10) | NO |  |
| 2 | `SUPPLIER_ID` | int(10) | NO |  |
| 3 | `STOCK_DAYS` | int(10) | NO |  |
| 4 | `PURCHASED` | bit | NO | ((0)) |
| 5 | `RECEIVED` | bit | NO | ((0)) |
| 6 | `RECEIPT_NO` | varchar(50) | YES |  |
| 7 | `ICMS_AMOUNT` | money(19) | NO | ((0)) |
| 8 | `RECEIPT_AMOUNT` | money(19) | NO | ((0)) |
| 9 | `ORDER_AMOUNT` | money(19) | NO | ((0)) |
| 10 | `GTA_AMOUNT` | money(19) | NO | ((0)) |
| 11 | `SHIP_AMOUNT` | money(19) | NO | ((0)) |
| 12 | `MEMO` | varchar(300) | YES |  |
| 13 | `APPROVED` | bit | NO | ((0)) |
| 14 | `APPROVE_USER` | varchar(20) | YES |  |
| 15 | `APPROVE_DATE` | smalldatetime | YES |  |
| 16 | `BOX_AMOUNT` | money(19) | NO | ((0)) |
| 17 | `RECEIPT_DATE` | smalldatetime | YES |  |

### `PURCHASE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 3 | `USER_ID` | varchar(20) | NO |  |
| 4 | `APPLICATION_ID` | varchar(8) | NO |  |
| 5 | `DAYS` | int(10) | NO |  |

### `RECEIPT`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `RECEIPT_NO` | int(10) | NO |  |
| 2 | `RECEIPT_FORM_NO` | int(10) | NO |  |
| 3 | `ORDER_ID` | int(10) | NO |  |
| 4 | `SHIPMENT` | tinyint(3) | NO |  |
| 5 | `DELIVERY_SUPPLIER_ID` | int(10) | YES |  |
| 6 | `MSG_ID` | smallint(5) | YES |  |
| 7 | `CFOP_ID` | tinyint(3) | NO |  |
| 8 | `CATEGORY` | tinyint(3) | YES |  |
| 9 | `SYS_CREATION_DATE` | datetime | NO |  |
| 10 | `TYPE` | varchar(1) | YES |  |
| 11 | `NF_AMOUNT` | money(19) | NO |  |

### `RECEIPT_CANCEL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `CANCEL_DATE` | smalldatetime | NO |  |
| 5 | `RECEIPT_NO` | int(10) | NO |  |
| 6 | `MEMO` | varchar(200) | YES |  |
| 7 | `RECEIPT_FORM` | int(10) | NO |  |

### `RECEIPT_IN`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 3 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |
| 4 | `USER_ID` | varchar(20) | NO |  |
| 5 | `APPLICATION_ID` | varchar(8) | NO |  |
| 6 | `RECEIPT_DATE` | smalldatetime | NO |  |
| 7 | `RECEIPT_NUMBER` | int(10) | NO |  |
| 8 | `RECEIPT_AMOUNT` | money(19) | NO |  |
| 9 | `ICMS_AMOUNT` | money(19) | NO |  |
| 10 | `MEMO` | varchar(200) | YES |  |

### `RECEIPT_IN_DATA`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CONVERSION` | decimal(7,5) | NO |  |
| 2 | `II` | money(19) | YES |  |
| 3 | `BASE_CALC` | money(19) | YES |  |
| 4 | `ICMS` | money(19) | YES |  |
| 5 | `PRODUCT_COST` | money(19) | NO |  |
| 6 | `SHIPMENT` | money(19) | YES |  |
| 7 | `IPI` | money(19) | YES |  |
| 8 | `PIS` | money(19) | YES |  |
| 9 | `COFINS` | money(19) | YES |  |
| 10 | `DA` | money(19) | YES |  |
| 11 | `ICMS_PERC` | smallint(5) | NO |  |
| 12 | `WEIGHT_GROSS` | int(10) | YES |  |
| 13 | `WEIGHT_NET` | int(10) | YES |  |
| 14 | `VOLUMES` | int(10) | YES |  |
| 15 | `ESPECIE` | varchar(50) | YES |  |
| 16 | `RECEIPT_NO` | int(10) | NO |  |
| 17 | `CFOP_ID` | tinyint(3) | NO |  |
| 18 | `SUPPLIER_ID` | int(10) | NO |  |
| 19 | `SHIPM_ORIGIN` | tinyint(3) | NO |  |
| 20 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 21 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |
| 22 | `APPLICATION_ID` | varchar(8) | NO |  |
| 23 | `USER_ID` | varchar(20) | NO |  |
| 24 | `INOUT` | varchar(1) | YES |  |
| 25 | `TYPE` | varchar(1) | YES |  |
| 26 | `OBS` | varchar(200) | YES |  |
| 27 | `NDI` | varchar(10) | YES |  |
| 28 | `DDI` | date | YES |  |
| 29 | `LOC_DESEMB` | varchar(50) | YES |  |
| 30 | `UF_DESEMB` | varchar(2) | YES |  |
| 31 | `DDESEMB` | date | YES |  |
| 32 | `NFE_RECEIPT` | varchar(100) | YES |  |
| 33 | `NFE_STATUS` | char(1) | YES |  |
| 34 | `NFE_KEY` | varchar(50) | YES |  |
| 35 | `NFE_PROTOCOL` | varchar(17) | YES |  |
| 36 | `NFE_CANCEL_PROTOCOL` | varchar(15) | YES |  |
| 37 | `NFE_EMAIL_COUNT` | tinyint(3) | YES |  |
| 38 | `DELIVERY_SUPPLIER_ID` | int(10) | YES |  |
| 39 | `NFE_PROTOCOL_RESULT` | varchar(4) | YES |  |
| 40 | `INTERNAL_RECEIPT` | int(10) | YES |  |
| 41 | `ST_AMOUNT` | money(19) | YES |  |
| 42 | `VERSION` | tinyint(3) | NO | ((1)) |
| 43 | `CSOSN` | varchar(3) | NO | ((101)) |
| 44 | `ST_BASE_CALC` | money(19) | NO | ((0)) |
| 45 | `NFE_REF` | varchar(50) | YES |  |
| 46 | `OTHER_AMOUNT` | money(19) | NO | ((0)) |

### `RECEIPT_IN_DETAILS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `RECEIPT_NO` | int(10) | NO |  |
| 2 | `PRODUCT_CODE` | varchar(15) | NO |  |
| 3 | `PRODUCT_NAME` | varchar(200) | NO |  |
| 4 | `QUANTITY` | decimal(9,3) | NO |  |
| 5 | `UNIT_PRICE` | money(19) | NO |  |
| 6 | `FISCAL_CLASS` | varchar(20) | NO |  |
| 7 | `IPI` | decimal(4,2) | NO |  |
| 8 | `ICMS` | decimal(4,2) | NO |  |

### `RECEIPT_MSG`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | smallint(5) | NO |  |
| 2 | `MESSAGE` | varchar(400) | NO |  |

### `RETURN_ORDER`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `ORDER_ID` | int(10) | NO |  |
| 2 | `RETURN_ID` | int(10) | NO |  |

### `RETURN_ORDER_DETAILS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PRODUCT_ID` | int(10) | NO |  |
| 2 | `ORDER_ID` | int(10) | NO |  |
| 3 | `QUANTITY` | decimal(9,3) | NO |  |

### `RETURN_ORDER_HIST`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 2 | `USER_ID` | varchar(20) | NO |  |
| 3 | `APPLICATION_ID` | varchar(8) | NO |  |
| 4 | `STATUS` | varchar(1) | NO |  |
| 5 | `RETURN_ID` | int(10) | NO |  |
| 6 | `PKId` | int(10) | NO |  |
| 7 | `AMOUNT` | money(19) | NO | ((0)) |

### `RETURNING`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 3 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |
| 4 | `USER_ID` | varchar(20) | NO |  |
| 5 | `APPLICATION_ID` | varchar(8) | NO |  |
| 6 | `STATUS` | varchar(1) | NO |  |
| 7 | `MEMO` | varchar(210) | YES |  |
| 8 | `MEMO_REFUSE` | varchar(210) | YES |  |
| 9 | `CREDIT_DATE` | smalldatetime | YES |  |

### `SEC_ACTIVITY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | smallint(5) | NO |  |
| 2 | `CODE` | varchar(8) | NO |  |
| 3 | `DESCRIPTION` | varchar(30) | NO |  |

### `SEC_ACTV_RULES`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `UserId` | uniqueidentifier | NO |  |
| 2 | `ACTV_ID` | smallint(5) | NO |  |
| 3 | `RULE_DATA` | varchar(300) | NO |  |

### `STATE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | tinyint(3) | NO |  |
| 2 | `NAME` | varchar(20) | NO |  |
| 3 | `CODE` | varchar(2) | NO |  |
| 4 | `CAPITAL` | varchar(20) | NO |  |
| 5 | `ICMS` | tinyint(3) | YES |  |
| 6 | `COUNTRY_ID` | int(10) | NO | ((1)) |
| 7 | `C_UF` | varchar(2) | YES |  |

### `STOCK_HISTORY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PRODUCT_ID` | int(10) | NO |  |
| 2 | `SYS_CREATION_DATE` | datetime | NO |  |
| 3 | `USER_ID` | varchar(20) | NO |  |
| 4 | `PREVIOUS` | decimal(9,3) | NO |  |
| 5 | `QUANTITY` | decimal(9,3) | NO |  |
| 6 | `MEMO` | varchar(100) | NO |  |
| 7 | `CLIENT_ID` | int(10) | YES |  |

### `STOCK_IN`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SUPPLIER_ID` | int(10) | NO |  |
| 3 | `SYS_CREATION_DATE` | datetime | NO |  |
| 4 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 5 | `USER_ID` | varchar(20) | NO |  |
| 6 | `APPLICATION_ID` | varchar(8) | NO |  |
| 7 | `EXTERNAL_PKID` | varchar(15) | YES |  |
| 8 | `DRIVER_COST` | money(19) | YES |  |
| 9 | `GTA_COST` | money(19) | YES |  |
| 10 | `BOX_COST` | money(19) | YES |  |
| 11 | `SHIP_COST` | money(19) | YES |  |
| 12 | `PRODUCTS_COST` | money(19) | NO |  |
| 13 | `CONVERSION_TAX` | decimal(4,2) | YES |  |
| 14 | `CREDIT_AMOUNT` | money(19) | YES |  |
| 15 | `COST_TRANSPORT` | decimal(4,2) | NO |  |
| 16 | `COST_TRANSPORT_SUP` | decimal(4,2) | NO |  |
| 17 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 18 | `STATUS` | varchar(1) | NO |  |
| 19 | `PRODUCT_TYPE` | tinyint(3) | NO |  |
| 20 | `NFE_IND` | bit | NO | ((0)) |
| 21 | `NFE_AMOUNT` | money(19) | NO | ((0)) |
| 22 | `II` | money(19) | NO | ((0)) |
| 23 | `ICMS` | money(19) | NO | ((0)) |
| 24 | `PIS` | money(19) | NO | ((0)) |
| 25 | `COFINS` | money(19) | NO | ((0)) |

### `STOCK_IN_DETAIL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `STOCK_IN_ID` | int(10) | NO |  |
| 2 | `PRODUCT_ID` | int(10) | NO |  |
| 3 | `QUANTITY` | decimal(9,3) | NO |  |
| 4 | `UNIT_COST_GROSS` | money(19) | NO |  |
| 5 | `DISCOUNT` | decimal(4,2) | YES |  |
| 6 | `UNIT_COST_NET` | money(19) | NO |  |
| 7 | `TOTAL_COST` | money(19) | NO |  |
| 8 | `CURRENCY_ID` | tinyint(3) | NO |  |
| 9 | `PREVIOUS` | decimal(9,3) | NO | ((0)) |
| 10 | `IPI` | decimal(4,2) | NO | ((0)) |
| 11 | `COST_TRANSPORT` | decimal(4,2) | NO | ((1)) |
| 12 | `UNIT_COST_FINAL` | money(19) | NO | ((0)) |
| 13 | `UNIT_MARKET_PRICE` | money(19) | NO | ((0)) |
| 14 | `PROFIT` | decimal(7,3) | NO | ((0)) |

### `SUPPLIER`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `SOCIAL_NAME` | varchar(100) | NO |  |
| 3 | `CNPJ` | varchar(20) | YES |  |
| 4 | `ADDRESS_STREET` | varchar(50) | YES |  |
| 5 | `ADDRESS_BLOCK` | varchar(50) | YES |  |
| 6 | `ADDRESS_NUMBER` | varchar(100) | YES |  |
| 7 | `PHONE1` | varchar(60) | YES |  |
| 8 | `PHONE2` | varchar(60) | YES |  |
| 9 | `PHONE3` | varchar(60) | YES |  |
| 10 | `ADDRESS_ZIPCODE` | varchar(9) | YES |  |
| 11 | `ADDRESS_STATE_ID` | tinyint(3) | NO |  |
| 12 | `ADDRESS_CITY_ID` | smallint(5) | NO |  |
| 13 | `BANK_INFO_BANK_ID` | tinyint(3) | YES |  |
| 14 | `BANK_INFO_ACC_NO` | varchar(15) | YES |  |
| 15 | `BANK_INFO_AGENCY` | varchar(15) | YES |  |
| 16 | `PAYMENT_METHOD_ID` | tinyint(3) | YES |  |
| 17 | `PAYTERM` | tinyint(3) | YES |  |
| 18 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 19 | `SYS_UPDATE_DATE` | datetime | YES |  |
| 20 | `APPLICATION_ID` | varchar(8) | NO |  |
| 21 | `USER_ID` | varchar(20) | NO |  |
| 22 | `FAX_NO` | varchar(60) | YES |  |
| 23 | `CELULAR` | varchar(60) | YES |  |
| 24 | `STATE_INSCR` | varchar(30) | YES |  |
| 25 | `OBS` | varchar(500) | YES |  |
| 26 | `CONTACT` | varchar(100) | YES |  |
| 27 | `DISCOUNT` | decimal(4,2) | YES | ((0)) |
| 28 | `BANK_INFO_NAME` | varchar(30) | YES |  |
| 29 | `SWFFIT_CODE` | varchar(30) | YES |  |
| 30 | `SUPPLIER_GROUP_ID` | int(10) | NO |  |
| 31 | `EMAIL` | varchar(50) | YES |  |
| 32 | `ACTIVE` | varchar(1) | NO | ('Y') |
| 33 | `COST_TRANSPORT` | decimal(4,2) | YES |  |
| 34 | `ADDRESS_COMPLEMENT` | varchar(150) | YES |  |
| 35 | `STOCK_DAYS` | tinyint(3) | YES |  |
| 36 | `GNRL_ORDERING` | bit | NO | ((0)) |
| 37 | `SALES_PERC` | tinyint(3) | YES |  |
| 38 | `ADMIN_PERC` | tinyint(3) | YES |  |
| 39 | `OPERATION_PERC` | tinyint(3) | YES |  |
| 40 | `PAYMENT_PLAN` | varchar(100) | YES |  |
| 41 | `FANTASY_NAME` | varchar(100) | YES |  |
| 42 | `PERSON_TYPE` | varchar(1) | NO | ('J') |

### `SUPPLIER_GROUP`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `NAME` | varchar(100) | NO |  |
| 3 | `HIDDEN` | varchar(1) | YES |  |

### `SYS_CONTROL`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `CODE` | varchar(50) | NO |  |
| 2 | `VALUE` | varchar(50) | NO |  |

### `sysdiagrams`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `name` | nvarchar(128) | NO |  |
| 2 | `principal_id` | int(10) | NO |  |
| 3 | `diagram_id` | int(10) | NO |  |
| 4 | `version` | int(10) | YES |  |
| 5 | `definition` | varbinary(MAX) | YES |  |

### `UNITS`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `NAME` | varchar(50) | NO |  |
| 3 | `DESCRIPTION` | varchar(100) | YES |  |
| 4 | `DECIMAL_IND` | bit | NO | ((0)) |
| 5 | `LABEL` | varchar(15) | NO |  |

### `WARRANTY`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `CLIENT_ID` | int(10) | NO |  |
| 3 | `SUP_NF` | varchar(50) | YES |  |
| 4 | `EB_NF` | varchar(50) | YES |  |
| 5 | `EB_CERT` | varchar(50) | YES |  |
| 6 | `SUP_CERT` | varchar(50) | YES |  |
| 7 | `MODEL` | varchar(50) | YES |  |
| 8 | `VEHICLE_TYPE` | varchar(1) | NO |  |
| 9 | `CHASSI_NO` | varchar(50) | YES |  |
| 10 | `BODY_NO` | varchar(50) | YES |  |
| 11 | `PLATE_NO` | varchar(8) | NO |  |
| 12 | `SERIAL_NO` | varchar(50) | YES |  |
| 13 | `INSTALLATION_DATE` | smalldatetime | NO |  |
| 14 | `EXPIRATION_DATE` | smalldatetime | NO |  |
| 15 | `INSTALLED_BY` | varchar(50) | YES |  |
| 16 | `PARTS_USED` | varchar(200) | YES |  |
| 17 | `MEMO` | varchar(200) | YES |  |
| 18 | `ORDER_ID` | int(10) | YES |  |
| 19 | `SYS_CREATION_DATE` | smalldatetime | NO |  |
| 20 | `SYS_UPDATE_DATE` | smalldatetime | YES |  |
| 21 | `USER_ID` | varchar(20) | NO |  |

### `ZONE`

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | `PKId` | int(10) | NO |  |
| 2 | `DESCRIPTION` | varchar(300) | NO |  |

## Foreign keys

| Table | Column | References |
|-------|--------|------------|
| ACTIVITY_ROLE | ACTV_ID | SEC_ACTIVITY.PKId |
| ACTIVITY_ROLE | ROLE_ID | aspnet_Roles.RoleId |
| aspnet_Membership | ApplicationId | aspnet_Applications.ApplicationId |
| aspnet_Membership | UserId | aspnet_Users.UserId |
| aspnet_Paths | ApplicationId | aspnet_Applications.ApplicationId |
| aspnet_PersonalizationAllUsers | PathId | aspnet_Paths.PathId |
| aspnet_PersonalizationPerUser | PathId | aspnet_Paths.PathId |
| aspnet_PersonalizationPerUser | UserId | aspnet_Users.UserId |
| aspnet_Profile | UserId | aspnet_Users.UserId |
| aspnet_Roles | ApplicationId | aspnet_Applications.ApplicationId |
| aspnet_Users | ApplicationId | aspnet_Applications.ApplicationId |
| aspnet_UsersInRoles | RoleId | aspnet_Roles.RoleId |
| aspnet_UsersInRoles | UserId | aspnet_Users.UserId |
| CALL | ZONE_ID | ZONE.PKId |
| CALL_ORDER | CALL_ID | CALL.PKId |
| CALL_ORDER | ORDER_ID | ORDER.PKId |
| CALL_STEP | CALL_ID | CALL.PKId |
| CAR | CLIENT_ID | CLIENT.PKId |
| CITY | STATE_ID | STATE.PKId |
| CLIENT_DELIVERY_SUPPLIER_LINK | CLIENT_ID | CLIENT.PKId |
| CLIENT_DELIVERY_SUPPLIER_LINK | DELIVERY_SUPPLIER_ID | SUPPLIER.PKId |
| CLIENT_SALES_AGENTS_LINK | CLIENT_ID | CLIENT.PKId |
| CLIENT_SALES_AGENTS_LINK | USER_ID | aspnet_Users.UserId |
| CST_CFOP | CFOP_ID | CFOP.PKId |
| CST_CFOP | CSTB_ID | CSTB.PKId |
| CST_CFOP | STATE_ID | STATE.PKId |
| CURRENCY_CONVERSION | SOURCE_CURRENCY_ID | CURRENCY.PKId |
| CURRENCY_CONVERSION | TARGET_CURRENCY_ID | CURRENCY.PKId |
| DELIVERY_SUPPLIER_LINK | DELIVERY_SUPPLIER_ID | SUPPLIER.PKId |
| DELIVERY_SUPPLIER_LINK | SUPPLIER_ID | SUPPLIER.PKId |
| DISCOUNT | CLIENT_ID | CLIENT.PKId |
| DISCOUNT | PRODUCT_GROUP_ID | PRODUCT_GROUP.PKId |
| DISCOUNT_HIST | ORDER_ID | ORDER.PKId |
| FINANCE_BILLS_TO_PAY | CURRENCY_ID | CURRENCY.PKId |
| FINANCE_BILLS_TO_PAY | PAYMENT_METHOD_ID | PAYMENT_METHOD.PKId |
| FINANCE_BILLS_TO_PAY | SUPPLIER_ID | SUPPLIER.PKId |
| FINANCE_BILLS_TO_PAY_DETAIL | FINANCE_BILL_ID | FINANCE_BILLS_TO_PAY.PKId |
| FINANCE_BTR | CLIENT_ID | CLIENT.PKId |
| FINANCE_BTR | CURRENCY_ID | CURRENCY.PKId |
| FINANCE_BTR | PAYMENT_METHOD_ID_ | PAYMENT_METHOD.PKId |
| FINANCE_BTR_DETAIL | FINANCE_BTR_ID | FINANCE_BTR.PKId |
| FINANCE_BTR_DETAIL | PAYMENT_METHOD_ID | PAYMENT_METHOD.PKId |
| FINANCE_BTR_DETAIL | PAYMENT_SUB_METHOD_ID | PAYMENT_SUB_METHOD.PKId |
| FINANCE_CHECK | FINANCE_BTR_ID | FINANCE_BTR_DETAIL.FINANCE_BTR_ID |
| FINANCE_CHECK | TERM_NO | FINANCE_BTR_DETAIL.TERM_NO |
| FINANCE_PAYMENT | FINANCE_BILL_ID | FINANCE_BILLS_TO_PAY_DETAIL.FINANCE_BILL_ID |
| FINANCE_PAYMENT | TERM_NO | FINANCE_BILLS_TO_PAY_DETAIL.TERM_NO |
| FINANCE_RECEIVE | FINANCE_BTR_ID | FINANCE_BTR_DETAIL.FINANCE_BTR_ID |
| FINANCE_RECEIVE | TERM_NO | FINANCE_BTR_DETAIL.TERM_NO |
| MARKET | CURRENCY_ID | CURRENCY.PKId |
| MARKET_PRODUCT | MARKET_ID | MARKET.PKId |
| MARKET_PRODUCT | PRODUCT_ID | PRODUCT.PKId |
| MARKET_USER | MARKET_ID | MARKET.PKId |
| MARKET_USER | USER_ID | aspnet_Users.UserId |
| ORDER | BTR_ID | FINANCE_BTR.PKId |
| ORDER | CAR_ID | CAR.PKId |
| ORDER | CLIENT_ID | CLIENT.PKId |
| ORDER_DETAILS | CURRENCY_ID | CURRENCY.PKId |
| ORDER_DETAILS | ORDER_ID | ORDER.PKId |
| ORDER_DETAILS | PRODUCT_ID | PRODUCT.PKId |
| ORDER_EXPORT | ORDER_ID | ORDER.PKId |
| PAYMENT_SUB_METHOD | PAYMENT_METHOD_ID | PAYMENT_METHOD.PKId |
| PRODUCT | CST_ID | CST.PKId |
| PRODUCT | CSTB_ID | CSTB.PKId |
| PRODUCT | CURRENCY_ID | CURRENCY.PKId |
| PRODUCT | FISCAL_CLASS_ID | FISCAL_CLASS.PKId |
| PRODUCT | GROUP_ID | PRODUCT_GROUP.PKId |
| PRODUCT | SIZE_ID | ANIMAL_SIZE.PKId |
| PRODUCT | UNIT_ID | UNITS.PKId |
| PRODUCT_GROUP | PRODUCT_CLASS_ID | PRODUCT_CLASS.PKId |
| PRODUCT_SUPPLIER_LINK | PRODUCT_ID | PRODUCT.PKId |
| PRODUCT_SUPPLIER_LINK | SUPPLIER_ID | SUPPLIER.PKId |
| PURCH_CONVERSION | PURCH_ID | PURCHASE.PKId |
| PURCH_DETAILS | CURRENCY_ID | CURRENCY.PKId |
| PURCH_DETAILS | PRODUCT_ID | PRODUCT.PKId |
| PURCH_DETAILS | PURCH_ID | PURCH_SUPPLIER.PURCH_ID |
| PURCH_DETAILS | SUPPLIER_ID | PURCH_SUPPLIER.SUPPLIER_ID |
| PURCH_SP_CLIENT | CLIENT_ID | CLIENT.PKId |
| PURCH_SP_CLIENT | PRODUCT_ID | PURCH_DETAILS.PRODUCT_ID |
| PURCH_SP_CLIENT | PURCH_ID | PURCH_DETAILS.PURCH_ID |
| PURCH_SP_CLIENT | SUPPLIER_ID | PURCH_DETAILS.SUPPLIER_ID |
| PURCH_STOCK | PURCH_ID | PURCH_SUPPLIER.PURCH_ID |
| PURCH_STOCK | SUPPLIER_ID | PURCH_SUPPLIER.SUPPLIER_ID |
| PURCH_SUPPLIER | PURCH_ID | PURCHASE.PKId |
| PURCH_SUPPLIER | SUPPLIER_ID | SUPPLIER.PKId |
| RECEIPT_IN_DATA | CFOP_ID | CFOP.PKId |
| RECEIPT_IN_DETAILS | RECEIPT_NO | RECEIPT_IN_DATA.RECEIPT_NO |
| RETURN_ORDER | RETURN_ID | RETURNING.PKId |
| RETURN_ORDER_DETAILS | ORDER_ID | RETURN_ORDER.ORDER_ID |
| RETURN_ORDER_HIST | RETURN_ID | RETURNING.PKId |
| SEC_ACTV_RULES | ACTV_ID | SEC_ACTIVITY.PKId |
| SEC_ACTV_RULES | UserId | aspnet_Users.UserId |
| STATE | COUNTRY_ID | COUNTRY.PKId |
| STOCK_IN | CURRENCY_ID | CURRENCY.PKId |
| STOCK_IN | SUPPLIER_ID | SUPPLIER.PKId |
| STOCK_IN_DETAIL | CURRENCY_ID | CURRENCY.PKId |
| STOCK_IN_DETAIL | PRODUCT_ID | PRODUCT.PKId |
| STOCK_IN_DETAIL | STOCK_IN_ID | STOCK_IN.PKId |
| SUPPLIER | ADDRESS_CITY_ID | CITY.PKId |
| SUPPLIER | ADDRESS_STATE_ID | STATE.PKId |
| SUPPLIER | BANK_INFO_BANK_ID | BANK.PKId |
| SUPPLIER | PAYMENT_METHOD_ID | PAYMENT_METHOD.PKId |
| SUPPLIER | SUPPLIER_GROUP_ID | SUPPLIER_GROUP.PKId |

## Views

- vw_aspnet_Applications
- vw_aspnet_MembershipUsers
- vw_aspnet_Profiles
- vw_aspnet_Roles
- vw_aspnet_Users
- vw_aspnet_UsersInRoles
- vw_aspnet_WebPartState_Paths
- vw_aspnet_WebPartState_Shared
- vw_aspnet_WebPartState_User

## Stored procedures

Includes aspnet_* membership/roles procedures and diagram helpers (`sp_*diagram*`). Query live schema via MCP `mssql_list_schema_objects` with `objectType='procedures'` for the full list.
