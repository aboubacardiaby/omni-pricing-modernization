028980 01  CUG02.                                                       00000010
028990     05 CUG02-C.                                                  00000020
029000        10 CUG02-CU-ACTIVE-CUST.                                  00000030
029010           15 CUG02-I-CUSTOMER                  PIC S9(8)         00000040
029020                                     VALUE     ZERO               00000050
029030                                     COMPUTATIONAL.               00000060
029040           15 CUG02-C-ACTIVE-CUST-STAT          PIC X(2)          00000070
029050                                     VALUE     SPACE.             00000080
029060           15 CUG02-D-ACTIVE-CUST-STAT          PIC X(10)         00000090
029070                                     VALUE     SPACE.             00000100
029080           15 CUG02-D-ACTIVE-CUST-STAT-FULL                       00000110
029090                                     REDEFINES                    00000120
029100              CUG02-D-ACTIVE-CUST-STAT.                           00000130
029110           16 CUG02-D-ACTIVE-CUST-STAT-MO-X.                      00000140
029120           17 CUG02-D-ACTIVE-CUST-STAT-MO       PIC 9(2).         00000150
029130           16 CUG02-D-ACTIVE-CUST-STAT-S1       PIC X(1).         00000160
029140           16 CUG02-D-ACTIVE-CUST-STAT-DY-X.                      00000170
029150           17 CUG02-D-ACTIVE-CUST-STAT-DY       PIC 9(2).         00000180
029160           16 CUG02-D-ACTIVE-CUST-STAT-S2       PIC X(1).         00000190
029170           16 CUG02-D-ACTIVE-CUST-STAT-YR-X.                      00000200
029180           17 CUG02-D-ACTIVE-CUST-STAT-YR       PIC 9(4).         00000210
029190           15 CUG02-D-ROW-CREATED               PIC X(10)         00000220
029200                                     VALUE     SPACE.             00000230
029210           15 CUG02-D-ROW-CREATED-FULL                            00000240
029220                                     REDEFINES                    00000250
029230              CUG02-D-ROW-CREATED.                                00000260
029240           16 CUG02-D-ROW-CREATED-MO-X.                           00000270
029250           17 CUG02-D-ROW-CREATED-MO            PIC 9(2).         00000280
029260           16 CUG02-D-ROW-CREATED-S1            PIC X(1).         00000290
029270           16 CUG02-D-ROW-CREATED-DY-X.                           00000300
029280           17 CUG02-D-ROW-CREATED-DY            PIC 9(2).         00000310
029290           16 CUG02-D-ROW-CREATED-S2            PIC X(1).         00000320
029300           16 CUG02-D-ROW-CREATED-YR-X.                           00000330
029310           17 CUG02-D-ROW-CREATED-YR            PIC 9(4).         00000340
029320           15 CUG02-D-ROW-LAST-CHANGED          PIC X(10)         00000350
029330                                     VALUE     SPACE.             00000360
029340           15 CUG02-D-ROW-LAST-CHANGED-FULL                       00000370
029350                                     REDEFINES                    00000380
029360              CUG02-D-ROW-LAST-CHANGED.                           00000390
029370           16 CUG02-D-ROW-LAST-CHANGED-MO-X.                      00000400
029380           17 CUG02-D-ROW-LAST-CHANGED-MO       PIC 9(2).         00000410
029390           16 CUG02-D-ROW-LAST-CHANGED-S1       PIC X(1).         00000420
029400           16 CUG02-D-ROW-LAST-CHANGED-DY-X.                      00000430
029410           17 CUG02-D-ROW-LAST-CHANGED-DY       PIC 9(2).         00000440
029420           16 CUG02-D-ROW-LAST-CHANGED-S2       PIC X(1).         00000450
029430           16 CUG02-D-ROW-LAST-CHANGED-YR-X.                      00000460
029440           17 CUG02-D-ROW-LAST-CHANGED-YR       PIC 9(4).         00000470
029450           15 CUG02-C-CUST-DELETE               PIC X(1)          00000480
029460                                     VALUE     SPACE.             00000490
029470           15 CUG02-F-CUST-FOR-PROFIT           PIC X(1)          00000500
029480                                     VALUE     SPACE.             00000510
029490           15 CUG02-I-CUST-DEA-NBR              PIC X(9)          00000520
029500                                     VALUE     SPACE.             00000530
029510           15 CUG02-Q-CUST-BEDS-TOTAL           PIC S9(5)         00000540
029520                                     VALUE     ZERO               00000550
029530                                     COMPUTATIONAL.               00000560
029540           15 CUG02-Q-CUST-BEDS-OCCUP           PIC S9(5)         00000570
029550                                     VALUE     ZERO               00000580
029560                                     COMPUTATIONAL.               00000590
029570           15 CUG02-C-BUSINESS                  PIC X(2)          00000600
029580                                     VALUE     SPACE.             00000610
029590           15 CUG02-I-DIVISION                  PIC X(2)          00000620
029600                                     VALUE     SPACE.             00000630
029610           EXEC SQL                                               00000640
029620             DECLARE CUG02 TABLE                                  00000650
029630                 (I_CUSTOMER           INTEGER                    00000660
029640                                       NOT NULL,                  00000670
029650                  C_ACTIVE_CUST_STAT   CHAR(002)                  00000680
029660                                       NOT NULL,                  00000690
029670                  D_ACTIVE_CUST_STAT   DATE                       00000700
029680                                       NOT NULL,                  00000710
029690                  D_ROW_CREATED        DATE                       00000720
029700                                       NOT NULL,                  00000730
029710                  D_ROW_LAST_CHANGED   DATE                       00000740
029720                                       NOT NULL,                  00000750
029730                  C_CUST_DELETE        CHAR(001)                  00000760
029740                                       NOT NULL,                  00000770
029750                  F_CUST_FOR_PROFIT    CHAR(001)                  00000780
029760                                       NOT NULL,                  00000790
029770                  I_CUST_DEA_NBR       CHAR(009)                  00000800
029780                                       NOT NULL,                  00000810
029790                  Q_CUST_BEDS_TOTAL    INTEGER                    00000820
029800                                       ,                          00000830
029810                  Q_CUST_BEDS_OCCUP    INTEGER                    00000840
029820                                       ,                          00000850
029830                  C_BUSINESS           CHAR(002)                  00000860
029840                                       NOT NULL,                  00000870
029850                  I_DIVISION           CHAR(002)                  00000880
029860                                       NOT NULL)                  00000890
029870           END-EXEC.                                              00000900
029880 01  CUG02-NULL-INDICATORS.                                       00000910
029890        10 CUG02-Q-CUST-BEDS-TOTAL-NI           PIC S9(4)         00000920
029900                                     VALUE     ZERO               00000930
029910                                     COMPUTATIONAL.               00000940
029920        10 CUG02-Q-CUST-BEDS-OCCUP-NI           PIC S9(4)         00000950
029930                                     VALUE     ZERO               00000960
029940                                     COMPUTATIONAL.               00000970
029950        10 CUG02-NO-NULL-VARIABLES              PIC S9(4)         00000980
029960                                     VALUE     +0002              00000990
029970                                     COMPUTATIONAL.               00001000
