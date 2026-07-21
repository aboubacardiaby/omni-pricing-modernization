025750 01  CUG11.                                                       00000001
025760     02 CUG11-C.                                                  00000002
025770        10 CUG11-CU-AC-BG-PRIORITY.                               00000003
025780           15 CUG11-I-ACCOUNT                   PIC S9(8)         00000004
025790                                     VALUE     ZERO               00000005
025800                                     COMPUTATIONAL.               00000006
025810           15 CUG11-D-ACCT-BG-PRI-EFF           PIC X(10)         00000007
025820                                     VALUE     SPACE.             00000008
025830           15 CUG11-D-ACCT-BG-PRI-EFF-FULL                        00000009
025840                                     REDEFINES                    00000010
025850              CUG11-D-ACCT-BG-PRI-EFF.                            00000011
025860           16 CUG11-D-ACCT-BG-PRI-EFF-MO-X.                       00000012
025870           17 CUG11-D-ACCT-BG-PRI-EFF-MO        PIC 9(2).         00000013
025880           16 CUG11-D-ACCT-BG-PRI-EFF-S1        PIC X(1).         00000014
025890           16 CUG11-D-ACCT-BG-PRI-EFF-DY-X.                       00000015
025900           17 CUG11-D-ACCT-BG-PRI-EFF-DY        PIC 9(2).         00000016
025910           16 CUG11-D-ACCT-BG-PRI-EFF-S2        PIC X(1).         00000017
025920           16 CUG11-D-ACCT-BG-PRI-EFF-YR-X.                       00000018
025930           17 CUG11-D-ACCT-BG-PRI-EFF-YR        PIC 9(4).         00000019
025940           15 CUG11-I-BUY-GROUP                 PIC S9(8)         00000020
025950                                     VALUE     ZERO               00000021
025960                                     COMPUTATIONAL.               00000022
025970           15 CUG11-S-BG-MEMBER                 PIC S9(8)         00000023
025980                                     VALUE     ZERO               00000024
025990                                     COMPUTATIONAL.               00000025
026000           15 CUG11-Q-ACCT-BG-PRIORITY          PIC S9(4)         00000026
026010                                     VALUE     ZERO               00000027
026020                                     COMPUTATIONAL.               00000028
026030           15 CUG11-Q-PREF-TIER-LEVEL           PIC S9(4)         00000029
026040                                     VALUE     ZERO               00000030
026050                                     COMPUTATIONAL.               00000031
026060           15 CUG11-D-BG-TIER-START             PIC X(10)         00000032
026070                                     VALUE     SPACE.             00000033
026080           15 CUG11-D-BG-TIER-START-FULL                          00000034
026090                                     REDEFINES                    00000035
026100              CUG11-D-BG-TIER-START.                              00000036
026110           16 CUG11-D-BG-TIER-START-MO-X.                         00000037
026120           17 CUG11-D-BG-TIER-START-MO          PIC 9(2).         00000038
026130           16 CUG11-D-BG-TIER-START-S1          PIC X(1).         00000039
026140           16 CUG11-D-BG-TIER-START-DY-X.                         00000040
026150           17 CUG11-D-BG-TIER-START-DY          PIC 9(2).         00000041
026160           16 CUG11-D-BG-TIER-START-S2          PIC X(1).         00000042
026170           16 CUG11-D-BG-TIER-START-YR-X.                         00000043
026180           17 CUG11-D-BG-TIER-START-YR          PIC 9(4).         00000044
026190           EXEC SQL                                               00000045
026200             DECLARE CUG11 TABLE                                  00000046
026210                 (I_ACCOUNT            INTEGER                    00000047
026220                                       NOT NULL,                  00000048
026230                  D_ACCT_BG_PRI_EFF    DATE                       00000049
026240                                       NOT NULL,                  00000050
026250                  I_BUY_GROUP          INTEGER                    00000051
026260                                       NOT NULL,                  00000052
026270                  S_BG_MEMBER          INTEGER                    00000053
026280                                       NOT NULL,                  00000054
026290                  Q_ACCT_BG_PRIORITY   SMALLINT                   00000055
026300                                       NOT NULL,                  00000056
026310                  Q_PREF_TIER_LEVEL    SMALLINT                   00000057
026320                                       NOT NULL,                  00000058
026330                  D_BG_TIER_START      DATE                       00000059
026340                                       NOT NULL)                  00000060
026350           END-EXEC.                                              00000061
026360 01  CUG11-NULL-INDICATORS.                                       00000062
026370        10 CUG11-NO-NULL-VARIABLES              PIC S9(4)         00000063
026380                                     VALUE     +0000              00000064
026390                                     COMPUTATIONAL.               00000065
