023880 01  CUG07.                                                       00000001
023890     02 CUG07-C.                                                  00000002
023900        10 CUG07-CU-BG-PRIORITY.                                  00000003
023910           15 CUG07-I-CUSTOMER                  PIC S9(8)         00000004
023920                                     VALUE     ZERO               00000005
023930                                     COMPUTATIONAL.               00000006
023940           15 CUG07-D-CUST-BG-PRI-EFF           PIC X(10)         00000007
023950                                     VALUE     SPACE.             00000008
023960           15 CUG07-D-CUST-BG-PRI-EFF-FULL                        00000009
023970                                     REDEFINES                    00000010
023980              CUG07-D-CUST-BG-PRI-EFF.                            00000011
023990           16 CUG07-D-CUST-BG-PRI-EFF-MO-X.                       00000012
024000           17 CUG07-D-CUST-BG-PRI-EFF-MO        PIC 9(2).         00000013
024010           16 CUG07-D-CUST-BG-PRI-EFF-S1        PIC X(1).         00000014
024020           16 CUG07-D-CUST-BG-PRI-EFF-DY-X.                       00000015
024030           17 CUG07-D-CUST-BG-PRI-EFF-DY        PIC 9(2).         00000016
024040           16 CUG07-D-CUST-BG-PRI-EFF-S2        PIC X(1).         00000017
024050           16 CUG07-D-CUST-BG-PRI-EFF-YR-X.                       00000018
024060           17 CUG07-D-CUST-BG-PRI-EFF-YR        PIC 9(4).         00000019
024070           15 CUG07-I-BUY-GROUP                 PIC S9(8)         00000020
024080                                     VALUE     ZERO               00000021
024090                                     COMPUTATIONAL.               00000022
024100           15 CUG07-S-BG-MEMBER                 PIC S9(8)         00000023
024110                                     VALUE     ZERO               00000024
024120                                     COMPUTATIONAL.               00000025
024130           15 CUG07-Q-CUST-BG-PRIORITY          PIC S9(4)         00000026
024140                                     VALUE     ZERO               00000027
024150                                     COMPUTATIONAL.               00000028
024160           15 CUG07-Q-PREF-TIER-LEVEL           PIC S9(4)         00000029
024170                                     VALUE     ZERO               00000030
024180                                     COMPUTATIONAL.               00000031
024190           15 CUG07-D-BG-TIER-START             PIC X(10)         00000032
024200                                     VALUE     SPACE.             00000033
024210           15 CUG07-D-BG-TIER-START-FULL                          00000034
024220                                     REDEFINES                    00000035
024230              CUG07-D-BG-TIER-START.                              00000036
024240           16 CUG07-D-BG-TIER-START-MO-X.                         00000037
024250           17 CUG07-D-BG-TIER-START-MO          PIC 9(2).         00000038
024260           16 CUG07-D-BG-TIER-START-S1          PIC X(1).         00000039
024270           16 CUG07-D-BG-TIER-START-DY-X.                         00000040
024280           17 CUG07-D-BG-TIER-START-DY          PIC 9(2).         00000041
024290           16 CUG07-D-BG-TIER-START-S2          PIC X(1).         00000042
024300           16 CUG07-D-BG-TIER-START-YR-X.                         00000043
024310           17 CUG07-D-BG-TIER-START-YR          PIC 9(4).         00000044
024320           EXEC SQL                                               00000045
024330             DECLARE CUG07 TABLE                                  00000046
024340                 (I_CUSTOMER           INTEGER                    00000047
024350                                       NOT NULL,                  00000048
024360                  D_CUST_BG_PRI_EFF    DATE                       00000049
024370                                       NOT NULL,                  00000050
024380                  I_BUY_GROUP          INTEGER                    00000051
024390                                       NOT NULL,                  00000052
024400                  S_BG_MEMBER          INTEGER                    00000053
024410                                       NOT NULL,                  00000054
024420                  Q_CUST_BG_PRIORITY   SMALLINT                   00000055
024430                                       NOT NULL,                  00000056
024440                  Q_PREF_TIER_LEVEL    SMALLINT                   00000057
024450                                       NOT NULL,                  00000058
024460                  D_BG_TIER_START      DATE                       00000059
024470                                       NOT NULL)                  00000060
024480           END-EXEC.                                              00000061
024490 01  CUG07-NULL-INDICATORS.                                       00000062
024500        10 CUG07-NO-NULL-VARIABLES              PIC S9(4)         00000063
024510                                     VALUE     +0000              00000064
024520                                     COMPUTATIONAL.               00000065
