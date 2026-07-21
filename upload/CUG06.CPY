003290 01  CUG06.                                                       00000010
003300     05 CUG06-C.                                                  00000020
003310        10 CUG06-CU-BG-PRI-VERS.                                  00000030
003320           15 CUG06-I-CUSTOMER                  PIC S9(8)         00000040
003330                                     VALUE     ZERO               00000050
003340                                     COMPUTATIONAL.               00000060
003350           15 CUG06-D-CUST-BG-PRI-EFF           PIC X(10)         00000070
003360                                     VALUE     SPACE.             00000080
003370           15 CUG06-D-CUST-BG-PRI-EFF-FULL                        00000090
003380                                     REDEFINES                    00000100
003390              CUG06-D-CUST-BG-PRI-EFF.                            00000110
003400           16 CUG06-D-CUST-BG-PRI-EFF-MO-X.                       00000120
003410           17 CUG06-D-CUST-BG-PRI-EFF-MO        PIC 9(2).         00000130
003420           16 CUG06-D-CUST-BG-PRI-EFF-S1        PIC X(1).         00000140
003430           16 CUG06-D-CUST-BG-PRI-EFF-DY-X.                       00000150
003440           17 CUG06-D-CUST-BG-PRI-EFF-DY        PIC 9(2).         00000160
003450           16 CUG06-D-CUST-BG-PRI-EFF-S2        PIC X(1).         00000170
003460           16 CUG06-D-CUST-BG-PRI-EFF-YR-X.                       00000180
003470           17 CUG06-D-CUST-BG-PRI-EFF-YR        PIC 9(4).         00000190
003480           15 CUG06-D-CUST-BG-PRI-EXP           PIC X(10)         00000200
003490                                     VALUE     SPACE.             00000210
003500           15 CUG06-D-CUST-BG-PRI-EXP-FULL                        00000220
003510                                     REDEFINES                    00000230
003520              CUG06-D-CUST-BG-PRI-EXP.                            00000240
003530           16 CUG06-D-CUST-BG-PRI-EXP-MO-X.                       00000250
003540           17 CUG06-D-CUST-BG-PRI-EXP-MO        PIC 9(2).         00000260
003550           16 CUG06-D-CUST-BG-PRI-EXP-S1        PIC X(1).         00000270
003560           16 CUG06-D-CUST-BG-PRI-EXP-DY-X.                       00000280
003570           17 CUG06-D-CUST-BG-PRI-EXP-DY        PIC 9(2).         00000290
003580           16 CUG06-D-CUST-BG-PRI-EXP-S2        PIC X(1).         00000300
003590           16 CUG06-D-CUST-BG-PRI-EXP-YR-X.                       00000310
003600           17 CUG06-D-CUST-BG-PRI-EXP-YR        PIC 9(4).         00000320
003610           EXEC SQL                                               00000330
003620             DECLARE CUG06 TABLE                                  00000340
003630                 (I_CUSTOMER           INTEGER                    00000350
003640                                       NOT NULL,                  00000360
003650                  D_CUST_BG_PRI_EFF    DATE                       00000370
003660                                       NOT NULL,                  00000380
003670                  D_CUST_BG_PRI_EXP    DATE                       00000390
003680                                       )                          00000400
003690           END-EXEC.                                              00000410
003700 01  CUG06-NULL-INDICATORS.                                       00000420
003710        10 CUG06-D-CUST-BG-PRI-EXP-NI           PIC S9(4)         00000430
003720                                     VALUE     ZERO               00000440
003730                                     COMPUTATIONAL.               00000450
003740        10 CUG06-NO-NULL-VARIABLES              PIC S9(4)         00000460
003750                                     VALUE     +0001              00000470
003760                                     COMPUTATIONAL.               00000480
