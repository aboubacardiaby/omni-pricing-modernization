012650 01  CUG10.                                                       00000010
012660     05 CUG10-C.                                                  00000020
012670        10 CUG10-CU-AC-BG-PRI-VERS.                               00000030
012680           15 CUG10-I-ACCOUNT                   PIC S9(8)         00000040
012690                                     VALUE     ZERO               00000050
012700                                     COMPUTATIONAL.               00000060
012710           15 CUG10-D-ACCT-BG-PRI-EFF           PIC X(10)         00000070
012720                                     VALUE     SPACE.             00000080
012730           15 CUG10-D-ACCT-BG-PRI-EFF-FULL                        00000090
012740                                     REDEFINES                    00000100
012750              CUG10-D-ACCT-BG-PRI-EFF.                            00000110
012760           16 CUG10-D-ACCT-BG-PRI-EFF-MO-X.                       00000120
012770           17 CUG10-D-ACCT-BG-PRI-EFF-MO        PIC 9(2).         00000130
012780           16 CUG10-D-ACCT-BG-PRI-EFF-S1        PIC X(1).         00000140
012790           16 CUG10-D-ACCT-BG-PRI-EFF-DY-X.                       00000150
012800           17 CUG10-D-ACCT-BG-PRI-EFF-DY        PIC 9(2).         00000160
012810           16 CUG10-D-ACCT-BG-PRI-EFF-S2        PIC X(1).         00000170
012820           16 CUG10-D-ACCT-BG-PRI-EFF-YR-X.                       00000180
012830           17 CUG10-D-ACCT-BG-PRI-EFF-YR        PIC 9(4).         00000190
012840           15 CUG10-D-ACCT-BG-PRI-EXP           PIC X(10)         00000200
012850                                     VALUE     SPACE.             00000210
012860           15 CUG10-D-ACCT-BG-PRI-EXP-FULL                        00000220
012870                                     REDEFINES                    00000230
012880              CUG10-D-ACCT-BG-PRI-EXP.                            00000240
012890           16 CUG10-D-ACCT-BG-PRI-EXP-MO-X.                       00000250
012900           17 CUG10-D-ACCT-BG-PRI-EXP-MO        PIC 9(2).         00000260
012910           16 CUG10-D-ACCT-BG-PRI-EXP-S1        PIC X(1).         00000270
012920           16 CUG10-D-ACCT-BG-PRI-EXP-DY-X.                       00000280
012930           17 CUG10-D-ACCT-BG-PRI-EXP-DY        PIC 9(2).         00000290
012940           16 CUG10-D-ACCT-BG-PRI-EXP-S2        PIC X(1).         00000300
012950           16 CUG10-D-ACCT-BG-PRI-EXP-YR-X.                       00000310
012960           17 CUG10-D-ACCT-BG-PRI-EXP-YR        PIC 9(4).         00000320
012970           EXEC SQL                                               00000330
012980             DECLARE CUG10 TABLE                                  00000340
012990                 (I_ACCOUNT            INTEGER                    00000350
013000                                       NOT NULL,                  00000360
013010                  D_ACCT_BG_PRI_EFF    DATE                       00000370
013020                                       NOT NULL,                  00000380
013030                  D_ACCT_BG_PRI_EXP    DATE                       00000390
013040                                       )                          00000400
013050           END-EXEC.                                              00000410
013060 01  CUG10-NULL-INDICATORS.                                       00000420
013070        10 CUG10-D-ACCT-BG-PRI-EXP-NI           PIC S9(4)         00000430
013080                                     VALUE     ZERO               00000440
013090                                     COMPUTATIONAL.               00000450
013100        10 CUG10-NO-NULL-VARIABLES              PIC S9(4)         00000460
013110                                     VALUE     +0001              00000470
013120                                     COMPUTATIONAL.               00000480
