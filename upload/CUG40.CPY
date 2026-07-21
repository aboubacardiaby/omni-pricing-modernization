000010 01  CUG40.                                                       00000001
000020****************************************                          00000002
000030****************************************                          00000003
000040     02 CUG40-C.                                                  00000004
000050        10 CUG40-ADJUST-TYPE-KEY.                                 00000005
000060           15 CUG40-I-ACCT-ADJUST-TYPE          PIC X(2)          00000006
000070                                     VALUE     SPACE.             00000007
000080           15 CUG40-C-ACCT-ADJ-LEVEL            PIC X(3)          00000008
000090                                     VALUE     SPACE.             00000009
000100           15 CUG40-D-ACT-ADJ-TYPE-EFF          PIC X(10)         00000010
000110                                     VALUE     SPACE.             00000011
000120           15 CUG40-D-ACT-ADJ-TYPE-EFF-FULL                       00000012
000130                                     REDEFINES                    00000013
000140              CUG40-D-ACT-ADJ-TYPE-EFF.                           00000014
000150           16 CUG40-D-ACT-ADJ-TYPE-EFF-MO-X.                      00000015
000160           17 CUG40-D-ACT-ADJ-TYPE-EFF-MO       PIC 9(2).         00000016
000170           16 CUG40-D-ACT-ADJ-TYPE-EFF-S1       PIC X(1).         00000017
000180           16 CUG40-D-ACT-ADJ-TYPE-EFF-DY-X.                      00000018
000190           17 CUG40-D-ACT-ADJ-TYPE-EFF-DY       PIC 9(2).         00000019
000200           16 CUG40-D-ACT-ADJ-TYPE-EFF-S2       PIC X(1).         00000020
000210           16 CUG40-D-ACT-ADJ-TYPE-EFF-YR-X.                      00000021
000220           17 CUG40-D-ACT-ADJ-TYPE-EFF-YR       PIC 9(4).         00000022
000230        10 CUG40-N-ACCT-ADJUST-TYPE             PIC X(30)         00000023
000240                                     JUSTIFIED RIGHT              00000024
000250                                     VALUE     SPACE.             00000025
000260        10 CUG40-N-ACCT-ADJ-LEVEL               PIC X(15)         00000026
000270                                     VALUE     SPACE.             00000027
000280        10 CUG40-D-ACT-ADJ-TYPE-EXP             PIC X(10)         00000028
000290                                     VALUE     SPACE.             00000029
000300        10 CUG40-D-ACT-ADJ-TYPE-EXP-FULL                          00000030
000310                                     REDEFINES                    00000031
000320           CUG40-D-ACT-ADJ-TYPE-EXP.                              00000032
000330        11 CUG40-D-ACT-ADJ-TYPE-EXP-MO-X.                         00000033
000340        12 CUG40-D-ACT-ADJ-TYPE-EXP-MO          PIC 9(2).         00000034
000350        11 CUG40-D-ACT-ADJ-TYPE-EXP-S1          PIC X(1).         00000035
000360        11 CUG40-D-ACT-ADJ-TYPE-EXP-DY-X.                         00000036
000370        12 CUG40-D-ACT-ADJ-TYPE-EXP-DY          PIC 9(2).         00000037
000380        11 CUG40-D-ACT-ADJ-TYPE-EXP-S2          PIC X(1).         00000038
000390        11 CUG40-D-ACT-ADJ-TYPE-EXP-YR-X.                         00000039
000400        12 CUG40-D-ACT-ADJ-TYPE-EXP-YR          PIC 9(4).         00000040
000410        10 CUG40-F-PRODUCT                      PIC X(1)          00000041
000420                                     VALUE     SPACE.             00000042
000430        10 CUG40-F-PROD-EXCLUSION               PIC X(1)          00000043
000440                                     VALUE     SPACE.             00000044
000450        10 CUG40-F-VENDOR                       PIC X(1)          00000045
000460                                     VALUE     SPACE.             00000046
000470        10 CUG40-F-VENDOR-EXCLUSION             PIC X(1)          00000047
000480                                     VALUE     SPACE.             00000048
000490        10 CUG40-F-CATEGORY                     PIC X(1)          00000049
000500                                     VALUE     SPACE.             00000050
000510        10 CUG40-F-CAT-EXCLUSION                PIC X(1)          00000051
000520                                     VALUE     SPACE.             00000052
000530        10 CUG40-F-PAY-FEE                      PIC X(1)          00000053
000540                                     VALUE     SPACE.             00000054
000550        10 CUG40-C-FEE-TYPE                     PIC X(1)          00000055
000560                                     VALUE     SPACE.             00000056
000570        10 CUG40-C-ADJ-SERV-TYPE                PIC X(2)          00000057
000580                                     VALUE     SPACE.             00000058
000581        10 CUG40-F-ADJ-TAX                      PIC X(1)          00000059
000600                                     VALUE     SPACE.             00000060
000610****************************************                          00000061
000620****************************************                          00000062
000630           EXEC SQL                                               00000063
000640             DECLARE CUG40 TABLE                                  00000064
000650                 (I_ACCT_ADJUST_TYPE   CHAR(002)                  00000065
000660                                       NOT NULL,                  00000066
000670                  C_ACCT_ADJ_LEVEL     CHAR(003)                  00000067
000680                                       NOT NULL,                  00000068
000690                  D_ACT_ADJ_TYPE_EFF   DATE                       00000069
000700                                       NOT NULL,                  00000070
000710                  N_ACCT_ADJUST_TYPE   CHAR(030)                  00000071
000720                                       NOT NULL,                  00000072
000730                  N_ACCT_ADJ_LEVEL     CHAR(015)                  00000073
000740                                       NOT NULL,                  00000074
000750                  D_ACT_ADJ_TYPE_EXP   DATE                       00000075
000760                                       ,                          00000076
000770                  F_PRODUCT            CHAR(001)                  00000077
000780                                       NOT NULL,                  00000078
000790                  F_PROD_EXCLUSION     CHAR(001)                  00000079
000800                                       NOT NULL,                  00000080
000810                  F_VENDOR             CHAR(001)                  00000081
000820                                       NOT NULL,                  00000082
000830                  F_VENDOR_EXCLUSION   CHAR(001)                  00000083
000840                                       NOT NULL,                  00000084
000850                  F_CATEGORY           CHAR(001)                  00000085
000860                                       NOT NULL,                  00000086
000870                  F_CAT_EXCLUSION      CHAR(001)                  00000087
000880                                       NOT NULL,                  00000088
000890                  F_PAY_FEE            CHAR(001)                  00000089
000900                                       NOT NULL,                  00000090
000910                  C_FEE_TYPE           CHAR(001)                  00000091
000920                                       NOT NULL,                  00000092
000930                  C_ADJ_SERV_TYPE      CHAR(002)                  00000093
000940                                       NOT NULL,                  00000094
000941                  F_ADJ_TAX            CHAR(001))                 00000095
000960                                                                  00000096
000970           END-EXEC.                                              00000097
000980 01  CUG40-NULL-INDICATORS.                                       00000098
000990****************************************                          00000099
001000****************************************                          00000100
001010        10 CUG40-D-ACT-ADJ-TYPE-EXP-NI          PIC S9(4)         00000101
001020                                     VALUE     ZERO               00000102
001030                                     COMPUTATIONAL.               00000103
001040        10 CUG40-C-FEE-TYPE-NI                  PIC S9(4)         00000104
001050                                     VALUE     ZERO               00000105
001060                                     COMPUTATIONAL.               00000106
001070        10 CUG40-NO-NULL-VARIABLES              PIC S9(4)         00000107
001080                                     VALUE     +0002              00000108
001090                                     COMPUTATIONAL.               00000109
