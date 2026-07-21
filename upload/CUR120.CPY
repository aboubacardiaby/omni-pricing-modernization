      ******************************************************************00000010
      * COPY BOOK FOR DATASTRUCTURE USED TO PASS DATA TO & RECEIVE FROM*00000020
      * SUBROUTINE CUP120/CUP121                                       *00000030
      * CREATED BY : SATHYA      DDI, FAX: 266-4388                    *00000040
      * CREATION DT: FEB 2001                                          *00000050
      * TOTAL LENGTH AT THE TIME OF INITIAL CREATION:     CHARS        *00000060
      *                                                                *00000070
      * NOTE:                                                          *00000080
      * -----                                                          *00000090
      * WHENEVER CHANGE IS MADE TO THIS DATA STRUCTURE , AFFECTING     *00000100
      * THE TOTAL LENGTH OF THIS RECORD, THE FOLLOWING NEED TO BE CHANG*00000110
      * -ED:                                                           *00000120
      * 1) LENGTH OF REC IN LINKAGE SECTION IN CUS120                  *00000130
      * 2) CUP120 & CUP121 ARE TO BE RECOMPILED                        *00000140
      *                                                                *00000150
      ******************************************************************00000160
      *----------------------------------------------------------------*00000170
      *                    CHANGE HISTORY                              *00000180
      *----------------------------------------------------------------*00000190
      * DATE   PGMR        DESCRIPTION                                 *00000200
BM0421*----------------------------------------------------------------*
BM0421* 04/21    BMISHR    WR 20-9002 PRICER ENHANCEMENT               *
NK0517*---------------------------------------------------------        -- 
NK0517* 05/17    NKIMBO    CR#17-0051  
NK0517*                    Changes to support DOD GEN V OMN                                                                
HR0415*----------------------------------------------------------------*
HR0415* 04/15    HRAI      CR14-0140
HR0415*                    ADDED TWO NEW FIELDS
HR0415*                    CUR120-F-UOM-ACT-GRP-FEE-SW AND
HR0415*                    CUR120-JIT-LUOM-CHRG-AMT
NK0813*----------------------------------------------------------------*
NK0813* 08/06/13 NKIMBO    IR#13-0361                                  *
NK0813*                    ADDED NEW FIELDS:                           *
NK0813*                    CUR120-JIT-LUM-FEE-PCT                      *
KS0912*----------------------------------------------------------------*
KS0912* 09/13/12 KSHEKA    CR#11-0093                                  *
KS0912*                    ADDED 2 NEW FIELDS CUR120-JIT-LUM-FEE-PCT & *
KS0912*                    CUR120-JIT-EXTRA-DELIV-FEE-PCT FOR STORING  *
KS0912*                    THE LUM & EXTRA DELIVERY FEES PERCENTAGES   *
      *----------------------------------------------------------------*00000210
      * 05/03/03 SATHYA    ADDED NEW FIELD CUR120-F-LUOM-ELIG-SW*       00000211
      *                    THIS INDICATES WHETHER AN ACCOUNT NEEDS TO  *00000212
      *                    BE CHARGED LUOM CHARGES ( REF: SA0403)      *00000213
      *                    INCDIDENT: 461097                           *00000214
      *----------------------------------------------------------------*00000220
          05  CUR120                            PIC X(360).             00000230
          05  CUR120-REDEF REDEFINES CUR120.                            00000240
            07 CUR120-RECEIVD-DATA.                                     00000250
              10  CUR120-I-ACCOUNT              PIC S9(08) USAGE COMP.  00000260
              10  CUR120-DATE                   PIC X(10).              00000270
      *              IN MM/DD/CCYY FORMAT                               00000280
              10  CUR120-SHIP-TO                PIC X(03).              00000290
              10  CUR120-ACCOUNT-TYPE           PIC X(01).              00000300
              10  CUR120-S-ACCOUNT              PIC X(06).              00000310
              10  CUR120-S-BR                   PIC X(02).              00000320
NK0517        10  CUR120-CREDIT-ACCT-ORDER-SW   PIC X(01).    
NK0517            88  CUR120-CREDIT-ACCT-ORDER       VALUE 'Y'.
BM0421        10  CUR120-CUSTOMER-NBR           PIC S9(10)
BM0421                                     COMPUTATIONAL-3.
BM0421*       10  FILLER                        PIC X(05).              00000330
            07 CUR120-RTND-DATA.                                        00000340
              10 CUR120-F-ST-JIT-CUSTOMER       PIC X(1).               00000350
              10 CUR120-JIT-PRICING-FIELDS.                             00000360
                15  CUR120-JIT-SERVICE-FEE         PIC X(1).            00000370
      *            88 CUR120-ADD-TO-ADJ-COST                            00000380
      *                                    VALUE     'C'.               00000390
      *            88 CUR120-ADD-TO-REG-PRICE                           00000400
      *                                    VALUE     'P'.               00000410
      *            88 CUR120-SHOW-ADJ-COST                              00000420
      *                                    VALUE     'A'.               00000430
      *            88 CUR120-SHOW-REG-PRICE                             00000440
      *                                    VALUE     'R'.               00000450
                15  CUR120-JIT-FEE-BREAK-OUT-SW    PIC X(1).            00000460
      *            88 CUR120-FEE-BREAK-OUT                              00000470
      *                                     VALUE     'Y'.              00000480
                15  CUR120-JIT-SERVICE-FEE-PCT     PIC S9(1)V9(4)       00000490
                                     COMPUTATIONAL-3.                   00000500
                15  CUR120-JIT-LABEL-CHRG-TYPE      PIC X(1).           00000510
      *            88 CUR120-LABEL-RATE-PER-ITEM                        00000520
      *                                        VALUE     'I'.           00000530
      *            88 CUR120-PCT-PER-LABEL                              00000540
      *                                        VALUE     'P'.           00000550
      *            88 CUR120-RATE-PER-LABEL                             00000560
      *                                        VALUE     'R'.           00000570
                15  CUR120-JIT-LABEL-CHRG-AMT       PIC S9(2)V9(4)      00000580
                                     COMPUTATIONAL-3.                   00000590
                15  CUR120-JIT-APPLY-CHRG-TYPE      PIC X(1).           00000600
      *            88 CUR120-APPLY-RATE-PER-ITEM                        00000610
      *                                         VALUE     'I'.          00000620
      *            88 CUR120-PCT-PER-APPLY                              00000630
      *                                         VALUE     'P'.          00000640
      *            88 CUR120-RATE-PER-APPLY                             00000650
      *                                         VALUE     'R'.          00000660
                15  CUR120-JIT-APPLY-CHRG-AMT       PIC S9(2)V9(4)      00000670
                                     COMPUTATIONAL-3.                   00000680
                15  CUR120-JIT-BREAK-CHRG-TYPE      PIC X(1).           00000690
      *           88 CUR120-PCT-PER-BREAK                               00000700
      *                                           VALUE     'P'.        00000710
      *           88 CUR120-RATE-PER-BREAK                              00000720
      *                                           VALUE     'R'.        00000730
                15  CUR120-JIT-BREAK-CHRG-AMT       PIC S9(2)V9(4)      00000740
                                     COMPUTATIONAL-3.                   00000750
HR0415          15  CUR120-JIT-LUOM-CHRG-AMT      PIC S9(2)V9(4)
HR0415                               COMPUTATIONAL-3.
SA0403          15  CUR120-F-LUOM-ELIG-SW   PIC X(01).                  00000760
KS0912          15  CUR120-JIT-LUM-FEE-PCT          PIC S9(1)V9(4)
KS0912                               COMPUTATIONAL-3.
KS0912          15  CUR120-JIT-EXTRA-DELIV-FEE-PCT  PIC S9(1)V9(4)
KS0912                               COMPUTATIONAL-3.
NK0813          15  CUR120-JIT-NON-OM-SLCT-FEE-PCT  PIC S9(1)V9(4)
NK0813                               COMPUTATIONAL-3.
HR0415          15  CUR120-F-LUOM-ACT-GRP-FEE       PIC X(1).
HR0415          15  CUR120-F-BRK-BULK-ACT-GRP-FEE   PIC X(1).
********************************************************
              10 CUR120-CMF-PRICING-FIELDS.                                 
     **          * BREAKBULK, LUM                          
SJ0722          15 CUR120-A-BREAKBULK-BB   PIC S9(7)V9(8) COMP-3. 
SJ0722          15 CUR120-P-BREAKBULK-BB   PIC S9(3)V9(4) COMP-3.
SJ0722          15 CUR120-FEE-SHRT-CODE-BB PIC X(02).
SJ0722          15 CUR120-SKU-CODE-BB      PIC X(04).
SJ0722          15 CUR120-BILLING-FRQ-BB   PIC X(02).
SJ0722             88 CUR120-DAILY-BILL-BB          VALUE 'DL'.
SJ0722             88 CUR120-DAILY-BILL-SEP-BB      VALUE 'DS'.
SJ0722             88 CUR120-MONTHLY-AUTO-BILL-BB   VALUE 'MA'.
SJ0722             88 CUR120-MONTHLY-MANUAL-BILL-BB VALUE 'MM'.
SJ0722          15 CUR120-FEE-TYPE-BB      PIC X(01).
SJ0722             88 CUR120-FEE-PER-LINE-BB        VALUE 'L'.
SJ0722             88 CUR120-FEE-PER-QTY-BB         VALUE 'Q'.
SJ0722             88 CUR120-FEE-PER-ORDER-BB       VALUE 'O'.
SJ0722             88 CUR120-FEE-PCT-SELL-BB        VALUE 'P'.
SJ0722             88 CUR120-FEE-PCT-COST-BB        VALUE 'C'.
SJ0722          15 CUR120-A-LUM-FEE-AMT-BB PIC S9(7)V9(8) COMP-3. 
SJ0722          15 CUR120-P-LUM-FEE-PCT-BB PIC S9(3)V9(4) COMP-3.
     **          * APPLY LABEL                       
SJ0722          15 CUR120-A-APPLY-LAB-AL   PIC S9(7)V9(8) COMP-3. 
SJ0722          15 CUR120-P-APPLY-LAB-AL   PIC S9(3)V9(4) COMP-3.
SJ0722          15 CUR120-FEE-SHRT-CODE-AL PIC X(02).
SJ0722          15 CUR120-SKU-CODE-AL      PIC X(04).
SJ0722          15 CUR120-BILLING-FRQ-AL   PIC X(02).
SJ0722             88 CUR120-DAILY-BILL-AL          VALUE 'DL'.
SJ0722             88 CUR120-DAILY-BILL-SEP-AL      VALUE 'DS'.
SJ0722             88 CUR120-MONTHLY-AUTO-BILL-AL   VALUE 'MA'.
SJ0722             88 CUR120-MONTHLY-MANUAL-BILL-AL VALUE 'MM'.
SJ0722          15 CUR120-FEE-TYPE-AL      PIC X(01).
SJ0722             88 CUR120-FEE-PER-LINE-AL        VALUE 'L'.
SJ0722             88 CUR120-FEE-PER-QTY-AL         VALUE 'Q'.
SJ0722             88 CUR120-FEE-PER-ORDER-AL       VALUE 'O'.
SJ0722             88 CUR120-FEE-PCT-SELL-AL        VALUE 'P'.
SJ0722             88 CUR120-FEE-PCT-COST-AL        VALUE 'C'.
********************************************************
SJ0722        10  FILLER                     PIC X(22).
              10  CUR120-GENERAL-RESPONSE-FLAG  PIC X(01).              00000770
      *            88 CUR120-ABEND    VALUE 'A'.                        00000780
      *            88 CUR120-ERROR    VALUE 'E'.                        00000790
      *            88 CUR120-WARNING  VALUE 'W'.                        00000800
              10  CUR120-GENERAL-RESPONSE-MSG   PIC X(80).              00000810
              10  CUR120-ABEND-MSG.                                     00000820
                  15  CUR120-ABEND-MESSAGE      PIC X(80).              00000830
                  15  CUR120-ABEND-SQLCODE      PIC S9(03).             00000840
                  15  CUR120-ABEND-MESSAGE-2    PIC X(05).              00000850
                  15  CUR120-ABEND-KEY-1        PIC X(15).              00000860
                  15  CUR120-ABEND-KEY-2        PIC X(15).              00000870
                  15  CUR120-ABEND-KEY-3        PIC X(15).              00000880
      *                                                                 00000890
