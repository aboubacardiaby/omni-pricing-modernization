      ******************************************************************
      ** MODIFICATION LOG                                              *
      ******************************************************************
NR0625*  06/2025   NEERAR  DMND0001266 HIDE UOMS FROM GUI INTERACE     *
NR0625******************************************************************
SJ0515** SJACOB   05/04/2015   CR# 14-0025 ARROW CATALOG CHANGE        *
SJ0515*                        ADDED NEW FIELD I_VENDOR_PART_NBR       *
      ******************************************************************
KS0115** KSHEKA   01/21/2015   CR#14-0140 : BREAK BULK FEE CALCULATION *
KS0115**                       ADDED NEW FIELD I_UOM_DESIGNATOR FOR    *
KS0115**                       DESIGNATING SELLING, BREAK BULK & LOW   *
KS0115**                       UOMS                                    *
KS0115******************************************************************00000007
002000           EXEC SQL                                               00000001
002100             DECLARE VNG02 TABLE                                  00000002
002200                 (I_VENDOR             CHAR(004)                  00000003
002300                                       NOT NULL,                  00000004
002400                  I_VND_PRODUCT        CHAR(008)                  00000005
002500                                       NOT NULL,                  00000006
002600                  C_VND_PROD_BASE_UM   CHAR(002)                  00000007
002700                                       NOT NULL,                  00000008
002800                  T_VND_PROD_UM_DESC   CHAR(005)                  00000009
002900                                       NOT NULL,                  00000010
003000                  T_VND_PROD_DESC_1    CHAR(035)                  00000011
003100                                       NOT NULL,                  00000012
003200                  T_VND_PROD_DESC_2    CHAR(035)                  00000013
003300                                       NOT NULL,                  00000014
003400                  C_VEND_VHA_PLUS      CHAR(001)                  00000015
003500                                       NOT NULL,                  00000016
003600                  I_VND_CATALOG_NBR    CHAR(012)                  00000017
003700                                       NOT NULL,                  00000018
                        C_PRODUCT_TYPE       CHAR(001) NOT NULL,        00000019
                        C_CUSTOM_IND         CHAR(001) NOT NULL,        00000020
                        C_PRIVATE_LBL        CHAR(001) NOT NULL,        00000021
RP0511                  F_PANDAC_ITEM        CHAR(001) NOT NULL,        00000021
RP0511                  F_PROD_CAT_IND       CHAR(001) NOT NULL,
KS0115                  F_UOM_DESIGNATOR     CHAR(001) NOT NULL,
SJ0515                  I_VENDOR_PART_NBR    CHAR(020) NOT NULL,
NR0625                  F_UNIT_OF_SALE_SW    CHAR(001) NOT NULL,
NR0625                  F_UNIT_OF_SALE_SW    CHAR(001) NOT NULL
                  )                                                     00000022
003800           END-EXEC.                                              00000023
000100 01  VNG02.                                                       00000024
000200     02 VNG02-C.                                                  00000025
000300        10 VNG02-VN-VEND-PRODUCT.                                 00000026
000400           15 VNG02-I-VENDOR                    PIC X(4)          00000027
000500                                     VALUE     SPACE.             00000028
000600           15 VNG02-I-VND-PRODUCT               PIC X(8)          00000029
000700                                     VALUE     SPACE.             00000030
000800           15 VNG02-C-VND-PROD-BASE-UM          PIC X(2)          00000031
000900                                     VALUE     SPACE.             00000032
001000           15 VNG02-T-VND-PROD-UM-DESC          PIC X(5)          00000033
001100                                     VALUE     SPACE.             00000034
001200           15 VNG02-T-VND-PROD-DESC-1           PIC X(35)         00000035
001300                                     VALUE     SPACE.             00000036
001400           15 VNG02-T-VND-PROD-DESC-2           PIC X(35)         00000037
001500                                     VALUE     SPACE.             00000038
001600           15 VNG02-C-VEND-VHA-PLUS             PIC X(1)          00000039
001700                                     VALUE     SPACE.             00000040
001800           15 VNG02-I-VND-CATALOG-NBR           PIC X(12)         00000041
001900                                     VALUE     SPACE.             00000042
                 15 VNG02-C-PRODUCT-TYPE              PIC X(1).         00000043
                 15 VNG02-C-CUSTOM-IND                PIC X(1)          00000044
                                           VALUE     SPACE.             00000045
                 15 VNG02-C-PRIVATE-LBL               PIC X(1)          00000046
                                           VALUE     SPACE.             00000047
NK0810           15 VNG02-F-PANDAC-ITEM               PIC X(1)          00000046
                                           VALUE     SPACE.             00000047
RP0511           15 VNG02-F-PROD-CAT-IND              PIC X(1)
RP0511                                     VALUE     SPACE.
KS0115           15 VNG02-F-UOM-DESIGNATOR            PIC X(1)
KS0115                                     VALUE     SPACE.             
SJ0515           15 VNG02-I-VENDOR-PART-NBR          PIC X(20)
SJ0515                                     VALUE     SPACE.
NR0625           15 VNG02-F-UNIT-OF-SALE-SW          PIC X(1)
NR0625                                     VALUE     SPACE.
NR0625           15 VNG02-F-UNIT-OF-USE-SW           PIC X(1)
NR0625                                     VALUE     SPACE.
003900 01  VNG02-NULL-INDICATORS.                                       00000050
004000        10 VNG02-NO-NULL-VARIABLES              PIC S9(4)         00000051
004100                                     VALUE     +0000              00000052
004200                                     COMPUTATIONAL.               00000060
