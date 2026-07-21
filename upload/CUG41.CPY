011760 01  CUG41.                                                       00000010
011770****************************************                          00000020
011780****************************************                          00000030
011790     02 CUG41-C.                                                  00000040
011800        10 CUG41-I-SERVICE-FEE-KEY.                               00000050
011820              20 CUG41-I-ACCOUNT                PIC S9(8)         00000070
011830                                     VALUE     ZERO               00000080
011840                                     COMPUTATIONAL.               00000090
011850           15 CUG41-IS-DEFINED-BY-ACCOU.                          00000100
011870              25 CUG41-I-ACCT-ADJUST-TYPE       PIC X(2)          00000110
011880                                     VALUE     SPACE.             00000120
011890              25 CUG41-C-ACCT-ADJ-LEVEL         PIC X(3)          00000130
011900                                     VALUE     SPACE.             00000140
011910              25 CUG41-D-ACT-ADJ-TYPE-EFF       PIC X(10)         00000150
011920                                     VALUE     SPACE.             00000160
011930              25 CUG41-D-ACT-ADJ-TYPE-EFF-FULL                    00000170
011940                                     REDEFINES                    00000180
011950                 CUG41-D-ACT-ADJ-TYPE-EFF.                        00000190
011960              26 CUG41-D-ACT-ADJ-TYPE-EFF-MO-X.                   00000200
011970              27 CUG41-D-ACT-ADJ-TYPE-EFF-MO    PIC 9(2).         00000210
011980              26 CUG41-D-ACT-ADJ-TYPE-EFF-S1    PIC X(1).         00000220
011990              26 CUG41-D-ACT-ADJ-TYPE-EFF-DY-X.                   00000230
012000              27 CUG41-D-ACT-ADJ-TYPE-EFF-DY    PIC 9(2).         00000240
012010              26 CUG41-D-ACT-ADJ-TYPE-EFF-S2    PIC X(1).         00000250
012020              26 CUG41-D-ACT-ADJ-TYPE-EFF-YR-X.                   00000260
012030              27 CUG41-D-ACT-ADJ-TYPE-EFF-YR    PIC 9(4).         00000270
012040           15 CUG41-D-SERVICE-EFF               PIC X(10)         00000280
012050                                     VALUE     SPACE.             00000290
012060           15 CUG41-D-SERVICE-EFF-FULL                            00000300
012070                                     REDEFINES                    00000310
012080              CUG41-D-SERVICE-EFF.                                00000320
012090           16 CUG41-D-SERVICE-EFF-MO-X.                           00000330
012100           17 CUG41-D-SERVICE-EFF-MO            PIC 9(2).         00000340
012110           16 CUG41-D-SERVICE-EFF-S1            PIC X(1).         00000350
012120           16 CUG41-D-SERVICE-EFF-DY-X.                           00000360
012130           17 CUG41-D-SERVICE-EFF-DY            PIC 9(2).         00000370
012140           16 CUG41-D-SERVICE-EFF-S2            PIC X(1).         00000380
012150           16 CUG41-D-SERVICE-EFF-YR-X.                           00000390
012160           17 CUG41-D-SERVICE-EFF-YR            PIC 9(4).         00000400
012170        10 CUG41-D-SERVICE-EXP                  PIC X(10)         00000410
012180                                     VALUE     SPACE.             00000420
012190        10 CUG41-D-SERVICE-EXP-FULL                               00000430
012200                                     REDEFINES                    00000440
012210           CUG41-D-SERVICE-EXP.                                   00000450
012220        11 CUG41-D-SERVICE-EXP-MO-X.                              00000460
012230        12 CUG41-D-SERVICE-EXP-MO               PIC 9(2).         00000470
012240        11 CUG41-D-SERVICE-EXP-S1               PIC X(1).         00000480
012250        11 CUG41-D-SERVICE-EXP-DY-X.                              00000490
012260        12 CUG41-D-SERVICE-EXP-DY               PIC 9(2).         00000500
012270        11 CUG41-D-SERVICE-EXP-S2               PIC X(1).         00000510
012280        11 CUG41-D-SERVICE-EXP-YR-X.                              00000520
012290        12 CUG41-D-SERVICE-EXP-YR               PIC 9(4).         00000530
012300        10 CUG41-P-SERVICE-FEE                  PIC S9(1)V9(4)    00000540
012310                                     VALUE     ZERO               00000550
012320                                     COMPUTATIONAL-3.             00000560
012330        10 CUG41-A-SERVICE-FEE                  PIC S9(5)V9(4)    00000561
012340                                     VALUE     ZERO               00000580
012350                                     COMPUTATIONAL-3.             00000581
011870        10 CUG41-C-RATE-TYPE                    PIC X(1)          00000582
011880                                     VALUE     SPACE.             00000583
011870        10 CUG41-C-APPLY-TO                     PIC X(1)          00000584
011880                                     VALUE     SPACE.             00000585
011870        10 CUG41-F-BREAK-FEE-LINE               PIC X(1)          00000586
011880                                     VALUE     SPACE.             00000587
BM0215        10 CUG41-F-UOM-ACT-GRP-FEE              PIC X(1)          00000586
BM0215                                     VALUE     SPACE.             00000587
012410****************************************                          00000600
012420           EXEC SQL                                               00000610
012430             DECLARE CUG41 TABLE                                  00000620
012440                 (I_ACCOUNT            INTEGER                    00000630
012450                                       NOT NULL,                  00000640
012460                  I_ACCT_ADJUST_TYPE   CHAR(002)                  00000650
012470                                       NOT NULL,                  00000660
012480                  C_ACCT_ADJ_LEVEL     CHAR(003)                  00000670
012490                                       NOT NULL,                  00000680
012500                  D_ACT_ADJ_TYPE_EFF   DATE                       00000690
012510                                       NOT NULL,                  00000700
012520                  D_SERVICE_EFF        DATE                       00000710
012530                                       NOT NULL,                  00000720
012540                  D_SERVICE_EXP        DATE                       00000730
012550                                       ,                          00000740
012560                  P_SERVICE_FEE        DECIMAL(005,04)            00000750
012570                                       NOT NULL,                  00000760
012580                  A_SERVICE_FEE        DECIMAL(009,04)            00000761
012590                                       NOT NULL,                  00000762
012480                  C_RATE_TYPE          CHAR(001)                  00000763
012490                                       NOT NULL,                  00000764
012480                  C_APPLY_TO           CHAR(001)                  00000765
012490                                       NOT NULL,                  00000766
012480                  F_BREAK_FEE_LINE     CHAR(001)                  00000767
012490                                       NOT NULL                   00000768
BM0215                  F_UOM_ACT_GRP_FEE    CHAR(001)                  00000767
BM0215                                       NOT NULL)                  00000768
012600           END-EXEC.                                              00000790
012610 01  CUG41-NULL-INDICATORS.                                       00000800
012620****************************************                          00000810
012630****************************************                          00000820
012640        10 CUG41-D-SERVICE-EXP-NI               PIC S9(4)         00000830
012650                                     VALUE     ZERO               00000840
012660                                     COMPUTATIONAL.               00000850
012670        10 CUG41-NO-NULL-VARIABLES              PIC S9(4)         00000860
012680                                     VALUE     +0001              00000870
012690                                     COMPUTATIONAL.               00000880
