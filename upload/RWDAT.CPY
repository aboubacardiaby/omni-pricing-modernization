      *******************************************************           00000010
      *  COPYBOOK NAME:  RWDAT                                          00000020
      *  DESCRIPTION: ADWDATER date processing                          00000030
      *  Generated using RescueWare(R)TM                                00000040
      *  Copyright(C) 1998-2001 Relativity Technologies, Inc.           00000050
      *******************************************************           00000060
       01  RWDAT.                                                       00000070
           10  RWDAT-REMAINDER              PIC S9(6)V9(3) USAGE COMP-3 00000080
                                            VALUE ZERO.                 00000090
           10  RWDAT-DATE-AREA.                                         00000100
               15  RWDAT-SUB-DATE-NON-CREL-SW                           00000110
                                            PIC X(1)                    00000120
                                            VALUE SPACE.                00000130
               15  RWDAT-SUB-DATE-MONTHS    PIC 9(2)                    00000140
                                            VALUE ZERO.                 00000150
               15  RWDAT-SUB-DATE-CURR-YEAR PIC 9(4)                    00000160
                                            VALUE ZERO.                 00000170
               15  RWDAT-SUB-DATE-PRE.                                  00000180
                   20  RWDAT-SUB-DATE-YR    PIC 9(4)                    00000190
                                            VALUE ZERO.                 00000200
                   20  RWDAT-SUB-DATE-MO    PIC 9(2)                    00000210
                                            VALUE ZERO.                 00000220
                   20  RWDAT-SUB-DATE-DY    PIC 9(2)                    00000230
                                            VALUE ZERO.                 00000240
               15  RWDAT-NUMBER-DAYS        PIC S9(9)      USAGE COMP-3 00000250
                                            VALUE ZERO.                 00000260
                                                                        00000270
               15  RWDAT-NUMBER-MONTHS      PIC S9(9)      USAGE COMP-3 00000280
                                            VALUE ZERO.                 00000290
               15  RWDAT-NUMBER-YEARS       PIC S9(5)      USAGE COMP-3 00000300
                                            VALUE ZERO.                 00000310
               15  RWDAT-ALPHA-MO           PIC X(3)                    00000320
                                            VALUE SPACE.                00000330
               15  RWDAT-NUMERIC-MO         PIC 9(2)                    00000340
                                            VALUE ZERO.                 00000350
               15  RWDAT-DATE.                                          00000360
                   20  RWDAT-DATE-DY        PIC 9(2)                    00000370
                                            VALUE ZERO.                 00000380
                   20  RWDAT-DATE-MO        PIC X(3)                    00000390
                                            VALUE SPACE.                00000400
                   20  RWDAT-DATE-YR        PIC 9(2)                    00000410
                                            VALUE ZERO.                 00000420
               15  RWDAT-DATER.                                         00000430
                   20  RWDAT-DATER-DY       PIC 9(2)                    00000440
                                            VALUE ZERO.                 00000450
                   20  RWDAT-DATE-S-DY      REDEFINES  RWDAT-DATER-DY.  00000460
                       25  RWDAT-DATE-S-DY1 PIC X(1).                   00000470
                       25  RWDAT-DATE-S-DY2 PIC X(1).                   00000480
                   20  RWDAT-DATER-MO       PIC X(3)                    00000490
                                            VALUE SPACE.                00000500
                   20  RWDAT-DATE-S-MO      REDEFINES  RWDAT-DATER-MO.  00000510
                       25  RWDAT-DATE-S-MO-X                            00000520
                                            PIC X(2).                   00000530
                       25 FILLER            PIC X(1).                   00000540
                   20  RWDAT-DATE-S-MON     REDEFINES  RWDAT-DATER-MO.  00000550
                       25  RWDAT-DATE-S-MO-N                            00000560
                                            PIC 9(2).                   00000570
                       25 FILLER            PIC X(1).                   00000580
                   20  RWDAT-DATER-YR       PIC 9(4)                    00000590
                                            VALUE ZERO.                 00000600
                   20  RWDAT-DATE-S-YR      REDEFINES  RWDAT-DATER-YR.  00000610
                       25  RWDAT-DATE-S-YR1 PIC X(2).                   00000620
                       25  RWDAT-DATE-S-YR2 PIC X(2).                   00000630
               15  RWDAT-DATE-DDMMYY.                                   00000640
                   20  RWDAT-DATE-DDMMYY-DY PIC 9(2)                    00000650
                                            VALUE ZERO.                 00000660
                   20  RWDAT-DATE-DDMMYY-MO PIC 9(2)                    00000670
                                            VALUE ZERO.                 00000680
                   20  RWDAT-DATE-DDMMYY-YR PIC 9(2)                    00000690
                                            VALUE ZERO.                 00000700
               15  RWDAT-DATE-DDMMYY-X      REDEFINES                   00000710
                   RWDAT-DATE-DDMMYY.                                   00000720
                   20  RWDAT-DATE-DDMMYY-N  PIC 9(6).                   00000730
               15  RWDAT-DATE-MMDDYY.                                   00000740
                   20  RWDAT-DATE-MMDDYY-MO PIC 9(2)                    00000750
                                            VALUE ZERO.                 00000760
                   20  RWDAT-DATE-MMDDYY-DY PIC 9(2)                    00000770
                                            VALUE ZERO.                 00000780
                   20  RWDAT-DATE-MMDDYY-YR PIC 9(2)                    00000790
                                            VALUE ZERO.                 00000800
               15  RWDAT-DATE-MMDDYY-X      REDEFINES                   00000810
                   RWDAT-DATE-MMDDYY.                                   00000820
                   20  RWDAT-DATE-MMDDYY-N  PIC 9(6).                   00000830
               15  RWDAT-DATE-YYMMDD.                                   00000840
                   20  RWDAT-DATE-YYMMDD-YR PIC 9(2)                    00000850
                                            VALUE ZERO.                 00000860
                   20  RWDAT-DATE-YYMMDD-MO PIC 9(2)                    00000870
                                            VALUE ZERO.                 00000880
                   20  RWDAT-DATE-YYMMDD-DY PIC 9(2)                    00000890
                                            VALUE ZERO.                 00000900
               15  RWDAT-DATE-YYMMDD-X      REDEFINES RWDAT-DATE-YYMMDD.00000910
                   20  RWDAT-DATE-YYMMDD-N  PIC 9(6).                   00000920
               15  RWDAT-DATE-YYMM.                                     00000930
                   20  RWDAT-DATE-YYMM-YR   PIC 9(2)                    00000940
                                            VALUE ZERO.                 00000950
                   20  RWDAT-DATE-YYMM-MO   PIC 9(2)                    00000960
                                            VALUE ZERO.                 00000970
               15  RWDAT-DATE-YYMM-X        REDEFINES RWDAT-DATE-YYMM.  00000980
                   20  RWDAT-DATE-YYMM-N    PIC 9(4).                   00000990
               15  RWDAT-DATE-MMYY.                                     00001000
                   20  RWDAT-DATE-MMYY-MO   PIC 9(2)                    00001010
                                            VALUE ZERO.                 00001020
                   20  RWDAT-DATE-MMYY-YR   PIC 9(2)                    00001030
                                            VALUE ZERO.                 00001040
               15  RWDAT-DATE-MMYY-X        REDEFINES  RWDAT-DATE-MMYY. 00001050
                   20  RWDAT-DATE-MMYY-N    PIC 9(4).                   00001060
               15  RWDAT-DATE-YYDDD.                                    00001070
                   20  RWDAT-DATE-YYDDD-YR  PIC 9(2)                    00001080
                                            VALUE ZERO.                 00001090
                   20  RWDAT-DATE-YYDDD-DY  PIC 9(3)                    00001100
                                            VALUE ZERO.                 00001110
               15  RWDAT-DATE-YYDDD-X       REDEFINES RWDAT-DATE-YYDDD. 00001120
                   20  RWDAT-DATE-YYDDD-N   PIC 9(5).                   00001130
               15  RWDAT-DATE-YYDDD-X2      REDEFINES RWDAT-DATE-YYDDD. 00001140
                   20  RWDAT-DATE-YYDDD-YR-X                            00001150
                                            PIC X(2).                   00001160
                   20  RWDAT-DATE-YYDDD-DY-X                            00001170
                                            PIC X(3).                   00001180
               15  RWDAT-DATE-MMDDY.                                    00001190
                   20  RWDAT-DATE-MMDDY-MO  PIC 9(2)                    00001200
                                            VALUE ZERO.                 00001210
                   20  RWDAT-DATE-MMDDY-DY  PIC 9(2)                    00001220
                                            VALUE ZERO.                 00001230
                   20  RWDAT-DATE-MMDDY-YR  PIC 9(1)                    00001240
                                            VALUE ZERO.                 00001250
               15  RWDAT-DATE-MMDDY-X       REDEFINES RWDAT-DATE-MMDDY. 00001260
                   20  RWDAT-DATE-MMDDY-N   PIC 9(5).                   00001270
               15  RWDAT-DATE-DDMMY.                                    00001280
                   20  RWDAT-DATE-DDMMY-DY  PIC 9(2)                    00001290
                                            VALUE ZERO.                 00001300
                   20  RWDAT-DATE-DDMMY-MO  PIC 9(2)                    00001310
                                            VALUE ZERO.                 00001320
                   20  RWDAT-DATE-DDMMY-YR  PIC 9(1)                    00001330
                                            VALUE ZERO.                 00001340
               15  RWDAT-DATE-DDMMY-X       REDEFINES RWDAT-DATE-DDMMY. 00001350
                   20  RWDAT-DATE-DDMMY-N   PIC 9(5).                   00001360
               15  RWDAT-DATE-YMMDD.                                    00001370
                   20  RWDAT-DATE-YMMDD-YR  PIC 9(1)                    00001380
                                            VALUE ZERO.                 00001390
                   20  RWDAT-DATE-YMMDD-MO  PIC 9(2)                    00001400
                                            VALUE ZERO.                 00001410
                   20  RWDAT-DATE-YMMDD-DY  PIC 9(2)                    00001420
                                            VALUE ZERO.                 00001430
               15  RWDAT-DATE-YMMDD-X       REDEFINES RWDAT-DATE-YMMDD. 00001440
                   20  RWDAT-DATE-YMMDD-N   PIC 9(5).                   00001450
               15  RWDAT-DATE-MMDDYR.                                   00001460
                   20  RWDAT-DATE-MMDDYR-MO PIC 9(2)                    00001470
                                            VALUE ZERO.                 00001480
                   20  RWDAT-DATE-MMDDYR-DY PIC 9(2)                    00001490
                                            VALUE ZERO.                 00001500
                   20  RWDAT-DATE-MMDDYR-YR PIC 9(4)                    00001510
                                            VALUE ZERO.                 00001520
               15  RWDAT-DATE-MMDDYR-X      REDEFINES RWDAT-DATE-MMDDYR.00001530
                   20  RWDAT-DATE-MMDDYR-N  PIC 9(8).                   00001540
               15  RWDAT-DATE-DDMMYR.                                   00001550
                   20  RWDAT-DATE-DDMMYR-DY PIC 9(2)                    00001560
                                            VALUE ZERO.                 00001570
                   20  RWDAT-DATE-DDMMYR-MO PIC 9(2)                    00001580
                                            VALUE ZERO.                 00001590
                   20  RWDAT-DATE-DDMMYR-YR PIC 9(4)                    00001600
                                            VALUE ZERO.                 00001610
               15  RWDAT-DATE-DDMMYR-X      REDEFINES RWDAT-DATE-DDMMYR.00001620
                   20  RWDAT-DATE-DDMMYR-N  PIC 9(8).                   00001630
               15  RWDAT-DATE-YRMMDD.                                   00001640
                   20  RWDAT-DATE-YRMMDD-YR PIC 9(4)                    00001650
                                            VALUE ZERO.                 00001660
                   20  RWDAT-DATE-YRMMDD-MO PIC 9(2)                    00001670
                                            VALUE ZERO.                 00001680
                   20  RWDAT-DATE-YRMMDD-DY PIC 9(2)                    00001690
                                            VALUE ZERO.                 00001700
               15  RWDAT-DATE-YRMMDD-X      REDEFINES RWDAT-DATE-YRMMDD.00001710
                   20  RWDAT-DATE-YRMMDD-N  PIC 9(8).                   00001720
               15  RWDAT-DATE-MMM.                                      00001730
                   20  RWDAT-DATE-MMM-MO    PIC X(3)                    00001740
                                            VALUE SPACE.                00001750
               15  RWDAT-DATE-MMMYR.                                    00001760
                   20  RWDAT-DATE-MMMYR-MO  PIC X(3)                    00001770
                                            VALUE SPACE.                00001780
                   20  RWDAT-DATE-MMMYR-YR  PIC 9(4)                    00001790
                                            VALUE ZERO.                 00001800
           10  RWDAT-DATE-FLAG              PIC X(1)                    00001810
                                            VALUE SPACE.                00001820
           10  RWDAT-DATE-FOUR-DIGIT-YR-SW  PIC X(1)                    00001830
                                            VALUE SPACE.                00001840
           10  RWDAT-2-DIGIT-CENTURY-BREAK  PIC S9(2)                   00001850
                                            VALUE ZERO.                 00001860
           10  RWDAT-LOCAL-2-DIGIT-CENTURY  PIC S9(2)                   00001870
                                            VALUE ZERO.                 00001880
           10  RWDAT-HOLD-CENTURY-YR        PIC S9(4)                   00001890
                                            VALUE ZERO.                 00001900
           10  RWDAT-MISSING-OK             PIC X(1)                    00001910
                                            VALUE SPACE.                00001920
           10  RWDAT-DAYS-SINCE             PIC S9(7) USAGE COMP-3      00001930
                                            VALUE ZERO.                 00001940
           10  RWDAT-DAYS-2                 PIC S9(7) USAGE COMP-3      00001950
                                            VALUE ZERO.                 00001960
           10  RWDAT-COMPUTE                PIC S9(9) USAGE COMP-3      00001970
                                            VALUE ZERO.                 00001980
           10  RWDAT-SUB1                   PIC S9(4) USAGE COMP        00001990
                                            VALUE ZERO.                 00002000
           10  RWDAT-LEAP-YEARS             PIC S9(4) USAGE COMP        00002010
                                            VALUE ZERO.                 00002020
           10  RWDAT-DATE-TBL-ALL.                                      00002030
               15 FILLER                    PIC X(14)                   00002040
                                            VALUE 'JANUARY  31000'.     00002050
               15 FILLER                    PIC X(14)                   00002060
                                            VALUE 'FEBRUARY 28031'.     00002070
               15 FILLER                    PIC X(14)                   00002080
                                            VALUE 'MARCH    31059'.     00002090
               15 FILLER                    PIC X(14)                   00002100
                                            VALUE  'APRIL    30090'.    00002110
               15 FILLER                    PIC X(14)                   00002120
                                            VALUE  'MAY      31120'.    00002130
               15 FILLER                    PIC X(14)                   00002140
                                            VALUE  'JUNE     30151'.    00002150
               15 FILLER                    PIC X(14)                   00002160
                                            VALUE  'JULY     31181'.    00002170
               15 FILLER                    PIC X(14)                   00002180
                                            VALUE  'AUGUST   31212'.    00002190
               15 FILLER                    PIC X(14)                   00002200
                                            VALUE  'SEPTEMBER30243'.    00002210
               15 FILLER                    PIC X(14)                   00002220
                                            VALUE  'OCTOBER  31273'.    00002230
               15 FILLER                    PIC X(14)                   00002240
                                            VALUE  'NOVEMBER 30304'.    00002250
               15 FILLER                    PIC X(14)                   00002260
                                            VALUE  'DECEMBER 31334'.    00002270
               15 FILLER                    PIC X(14)                   00002280
                                            VALUE  'XXXXXXXXX99366'.    00002290
           10  RWDAT-DATE-TBL               REDEFINES                   00002300
               RWDAT-DATE-TBL-ALL.                                      00002310
               15  RWDAT-DATE-TBLS                                      00002320
                                            OCCURS 13 TIMES.            00002330
                   20  RWDAT-DATE-TBL-MO.                               00002340
                       25  RWDAT-ABBR-MO    PIC X(3).                   00002350
                       25 FILLER            PIC X(6).                   00002360
                   20  RWDAT-DATE-TBL-DAYS  PIC 9(2).                   00002370
                   20  RWDAT-ACCUM          PIC 9(3).                   00002380
           10  RWDAT-DATE-FULL.                                         00002390
               15  RWDAT-DATE-FULL-MO       PIC X(10)                   00002400
                                            VALUE  SPACE.               00002410
               15  RWDAT-DATE-FULL-DY       PIC Z9.                     00002420
               15  RWDAT-DATE-FULL-COMMA    PIC X(2)                    00002430
                                            VALUE  ' ,'.                00002440
               15  RWDAT-DATE-FULL-CENT     PIC 9(2)                    00002450
                                            VALUE  ZERO.                00002460
               15  RWDAT-DATE-FULL-YR       PIC 9(2)                    00002470
                                            VALUE  ZERO.                00002480
           10  RWDAT-DAY-OF-WEEK-NO         PIC 9(1)                    00002490
                                            VALUE  ZERO.                00002500
           10  RWDAT-DAY-OF-WEEK            PIC X(9)                    00002510
                                            VALUE  SPACE.               00002520
