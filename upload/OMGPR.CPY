000001****************************************                          00000100
000002* OMGPR    - REUSE-OMNI_2.0_PRICING_WS                            00000200
000003* CHANGE LOG:                                                     00000300
DP0722*----------------------------------------------------------------
DP0722* DPALEK  JUL 2022  WR 22-9470 PRICER ENHANCEMENT 2022 SCOPE    
KS0722* KSHEKA  
BM0722* BMISHR
SJ0722* SJACOB 
BM0821* ----------------------------------------------------------------
BM0821* BMISHR  AUG 2021  IR 20-0065 PRICER ENHANCEMENT PHASE 2 REQ 1  
SJ0821* SJACOB                        PHASE 2 REQ 6
BM0421*----------------------------------------------------------------
BM0421* BMISHR  APRIL 2021  WR 20-9002 PRICER ENHANCEMENT     
NK0518*---------------------------------------------------------------- 
NK0518* NKIMBO  MAY 2018  CR 15-0118 DUAL VENDOR  
NK0518*    -  Changes to support DUAL CONTRACT VENDORS
NK0517*---------------------------------------------------------------- 
NK0517* NKIMBO  MAY 2017  CR#17-0051 Changes to support DOD GEN V OMN   
NK0517* DOD-  Changes to support DOD GEN V OMN                         
NK0615*---------------------------------------------------------------- 
NK0615* NKIMBO  JUN 2015  CR#15-####      
NK0615* NDP-   NET DELIVERED PRICING 
NK0415* ----------------------------------------------------------------
NK0415* NKIMBO  APR 2015  CR#14-0140      
NK0415* BREAK BULK - LUOM -  REMOVE LOW VEL - MODIFY LUOM AND REUSE BOTH
NK0415*  FOR THIS C R ENHANCEMENTS       
NK0813* ----------------------------------------------------------------
NK0813* NKIMBO  AUG 2013  CR#13-0361
NK0813* DC FORMULARY PROJECT - ADD  NON OM SELECT FIELDS
NK0813*  ****NOTE - ALL FILLER HAS BEEN USED.. . WILL NEED TO EXPAND
NK0813*           FD TO INCREASE SIZE
KS0912* ----------------------------------------------------------------
KS0912* KSHEKA SEPT 2012  CR#11-0093
KS0912* INTRODUCED 2 NEW FIELDS OMGPR-JIT-EXTRA-DELIV-FEE-PCT &
KS0912* OMGPR-JIT-LUM-FEE-PCT TO INPUT PRICING FIELDS AND 2 NEW FIELDS
KS0912* OMGPR-A-JIT-LUM-FEE & OMGPR-A-JIT-EXTRA-DELIV-FEE TO OUTPUT
KS0912* PRICING FIELDS. REMOVED THE OMGPR-T-OUTPUT-FILLER FIELD AND
KS0912* REDUCED THE LENGTH OF OMGPR-T-INPUT-FILLER FIELD TO 27.
BM0612* ----------------------------------------------------------------
BM0612* BMISHR JUNE 2012  CR12-0046
BM0612* INSERTED A NEW FIELD OMGPR-VAR-I-BUY-GROUP ON INPUT SIDE
KS0411* ----------------------------------------------------------------
KS0411* KSHEKA APR 2011   CR10-0142
KS0411* ADD 5 NEW FIELDS TO ALLOCATE FREIGHT EXEMPTION FLAGS
000004* ----------------------------------------------------------------00000400
BN1211* BNARAY DEC 2010   CR#10-0121                                    00000400
BN1211* CUSTOMER ADJUSTMENTS DISTRIBUTION FEE BILLING FOR VENDOR JIT    00000400
BN1211* ADDED 4 FIELDS TO RECOGNIZE THE JIT FEE TO PASS TO A6X06        00000400
000004* ----------------------------------------------------------------00000400
000005* NKIMBO APR 2008    CR07-0139                                    00000500
000006* ADD ADDITIONAL OUTPUT FIELDS TO COPYBOOK                        00000600
000007* ----------------------------------------------------------------00000610
000008* SATHYA MAY 2004    CR03-0150                                    00000620
000009* INSERTED NEW FLDS: 1) OMGPR-F-CONT-EXCL-SW ON OUTPUT SIDE       00000630
000010*     THIS CAN HAVE VALUE OF "Y" - TO INDICATE CONTRACT EXCLUDED  00000700
      *     "N" OR SPACES TO INDICATE NO CONTRACT EXCLUSIONS ENCOUNTERED00000800
000006* 2)  OMGPR-F-INP-CNT-EXCL-SW  ON INPUT SIDE                      00000900
000006*     THIS CAN HAVE VALUE OF "Y" - TO INDICATE ACT/SHIP TO COMBINA00001000
      *     TION HAS ATLEAST 1 EXCLUSION                                00001100
000005* ----------------------------------------------------------------00001200
000006* SATHYA 05/03/2003  INCIDENT: 461097                             00001300
000006* INSERTED NEW FLD: OMGPR-F-LUOM-ELIG-SW                          00001400
000006*     THIS CAN HAVE VALUE OF "Y", "N" OR BLANKS INDICATING        00001500
      *     WHETHER AN ACCOUNT NEEDS TO BE CHARGED LUOM CHARGES         00001600
000005* ----------------------------------------------------------------00001700
000005* ----------------------------------------------------------------00001800
000006* SATHYA 08/15/2001  PROJECT: VNDR COST ADJ CHARGES               00001900
000006* INSERTED NEW FLDS:   OMGPR-P-VNDCOSTADJ , OMGPR-C-VNDCOSTADJ    00002000
000005* ----------------------------------------------------------------00002100
000005* ----------------------------------------------------------------00002200
000006* SATHYA 12/15/1999  PROJECT: ELIMINATION OF OMNI1 CONTRACTS      00002300
000006* INSERTED A NEW FIELD OMGPR-F-SPECIAL-CONTRACT                   00002400
000006* MOVED OMGPR-F-BYPASS-LUOM OUT OF DATA-FROM-CUP100               00002500
000005* ----------------------------------------------------------------00002600
000006* SATHYA 11/15/1999  PROJECT: BG LOW UOM                          00002700
000007* INSERTED THE FOLLOWING FIELDS AT THE END OF DATA-FROM-CUP100:   00002800
000008*     OMGPR-C-LUOM-EXCL                                           00002900
000008*     OMGPR-F-LUOM-VEND-EXCL                                      00003000
000008*     OMGPR-I-BUY-GROUP-LUOM                                      00003100
000009*     OMGPR-D-LOW-UOM-EFF                                         00003200
000009*     OMGPR-F-BYPASS-LUOM                                         00003300
000010* NOTE: THE SAME CHANGE IS MADE TO DATA STRUCTURE REUSE OMNI 2.0 P00003400
000011*       RICING WS IN PRICER ENCYCLOPEDIA.                         00003500
000012* ----------------------------------------------------------------00003600
000005* ----------------------------------------------------------------00003700
000006* SATHYA 08/02/1999                                               00003800
000007* INSERTED THE FIELD  AT THE END OF DATA-FROM-CUP100:             00003900
000008*     OMGPR-P-LOW-UOM                                             00004000
000007* INSERTED TWO FIELDS AFTER FILLER AT THE END OF DATA-FROM-CUP100:00004100
000009*     OMGPR-F-LOW-UOM                                             00004200
      *     OMGPR-F-SPECIAL-CONTRACT                                    00004300
000010* NOTE: THE SAME CHANGE IS MADE TO DATA STRUCTURE REUSE OMNI 2.0 P00004400
000011*       RICING WS IN PRICER ENCYCLOPEDIA.                         00004500
000012* ----------------------------------------------------------------00004600
000013****************************************                          00004700
000014 01  OMGPR.                                                       00004800
000015****************************************                          00004900
000016****************************************                          00005000
000017     02 OMGPR-C.                                                  00005100
000018        10 OMGPR-PRC-INPUT-FIELDS.                                00005200
000019           15 OMGPR-I-DIVISION                  PIC X(2)          00005300
000020                                     VALUE     SPACE.             00005400
000021           15 OMGPR-S-ACCOUNT                   PIC X(6)          00005500
000022                                     VALUE     SPACE.             00005600
000023           15 OMGPR-I-VENDOR                    PIC X(4)          00005700
000024                                     VALUE     SPACE.             00005800
000025           15 OMGPR-I-VND-PRODUCT               PIC X(8)          00005900
000026                                     VALUE     SPACE.             00006000
000027           15 OMGPR-Q-ORD-LIN-ORDERED           PIC S9(7)         00006100
000028                                     VALUE     ZERO               00006200
000029                                     COMPUTATIONAL.               00006300
000030           15 OMGPR-C-ORD-LIN-CUST-UOM          PIC X(2)          00006400
000031                                     VALUE     SPACE.             00006500
000032           15 OMGPR-C-SHIP-TO-SUFFIX            PIC X(3)          00006600
000033                                     VALUE     SPACE.             00006700
000034           15 OMGPR-C-BILL-TO-SUFFIX            PIC X(3)          00006800
000035                                     VALUE     SPACE.             00006900
000036           15 OMGPR-D-PRICING                   PIC X(10)         00007000
000037                                     VALUE     SPACE.             00007100
NK0615           15 OMGPR-CUST-TYPE                   PIC X(01)
NK0615                                     VALUE      SPACE.
NK0615              88 OMGPR-ECOMMERCE-PRICING       
NK0615                                     VALUE      'Y'.  
NK0517           15 OMGPR-CREDIT-ACCT-ORDER           PIC X(1)
NK0517                                     VALUE      SPACE.               
000038           15 OMGPR-JIT-DATA.                                     00007200
000039              20 OMGPR-JIT-PRICING-FIELDS.                        00007300
000040              25 OMGPR-JIT-SERVICE-FEE          PIC X(1)          00007400
000041                                     VALUE     SPACE.             00007500
000042                 88 OMGPR-ADD-TO-ADJ-COST                         00007600
000043                                     VALUE     'C'.               00007700
000044                 88 OMGPR-ADD-TO-REG-PRICE                        00007800
000045                                     VALUE     'P'.               00007900
000046                 88 OMGPR-SHOW-ADJ-COST                           00008000
000047                                     VALUE     'A'.               00008100
000048                 88 OMGPR-SHOW-REG-PRICE                          00008200
000049                                     VALUE     'R'.               00008300
000050              25 OMGPR-JIT-FEE-BREAK-OUT-SW     PIC X(1)          00008400
000051                                     VALUE     SPACE.             00008500
000052                 88 OMGPR-FEE-BREAK-OUT                           00008600
000053                                     VALUE     'Y'.               00008700
000054              25 OMGPR-JIT-SERVICE-FEE-PCT      PIC S9(1)V9(4)    00008800
000055                                     VALUE     ZERO               00008900
000056                                     COMPUTATIONAL-3.             00009000
000057              25 OMGPR-JIT-LABEL-CHRG-TYPE      PIC X(1)          00009100
000058                                     VALUE     SPACE.             00009200
000059                 88 OMGPR-LABEL-RATE-PER-ITEM                     00009300
000060                                     VALUE     'I'.               00009400
000061                 88 OMGPR-PCT-PER-LABEL                           00009500
000062                                     VALUE     'P'.               00009600
000063                 88 OMGPR-RATE-PER-LABEL                          00009700
000064                                     VALUE     'R'.               00009800
000065              25 OMGPR-JIT-LABEL-CHRG-AMT       PIC S9(2)V9(4)    00009900
000066                                     VALUE     ZERO               00010000
000067                                     COMPUTATIONAL-3.             00010100
000068              25 OMGPR-JIT-APPLY-CHRG-TYPE      PIC X(1)          00010200
000069                                     VALUE     SPACE.             00010300
000070                 88 OMGPR-APPLY-RATE-PER-ITEM                     00010400
000071                                     VALUE     'I'.               00010500
000072                 88 OMGPR-PCT-PER-APPLY                           00010600
000073                                     VALUE     'P'.               00010700
000074                 88 OMGPR-RATE-PER-APPLY                          00010800
000075                                     VALUE     'R'.               00010900
000076              25 OMGPR-JIT-APPLY-CHRG-AMT       PIC S9(2)V9(4)    00011000
000077                                     VALUE     ZERO               00011100
000078                                     COMPUTATIONAL-3.             00011200
000079              25 OMGPR-JIT-BREAK-CHRG-TYPE      PIC X(1)          00011300
000080                                     VALUE     SPACE.             00011400
000081     88 OMGPR-PCT-PER-BREAK                                       00011500
000082                                     VALUE     'P'.               00011600
000083     88 OMGPR-RATE-PER-BREAK                                      00011700
000084                                     VALUE     'R'.               00011800
NK0415*     2 FIELDS BELOW  ARE USED TO STORE CADJ2 FEES
000085              25 OMGPR-JIT-BREAK-CHRG-AMT       PIC S9(2)V9(4)    00011900
000086                                     VALUE     ZERO               00012000
000087                                     COMPUTATIONAL-3.             00012100
NK0415              25 OMGPR-JIT-LUOM-CHRG-AMT        PIC S9(2)V9(4)    00011900
NK0415                                     VALUE     ZERO               00012000
NK0415                                     COMPUTATIONAL-3.             00012100
000088              20 OMGPR-JIT-ITEM-LABEL-SW        PIC X(1)          00012200
000089                                     VALUE     SPACE.             00012300
000090     88 OMGPR-JIT-ITEM-LABEL                                      00012400
000091                                     VALUE     'Y'.               00012500
000092              20 OMGPR-JIT-ITEM-LABEL-QTY       PIC S9(5)         00012600
000093                                     VALUE     ZERO               00012700
000094                                     COMPUTATIONAL-3.             00012800
000095              20 OMGPR-JIT-ITEM-APPLY-LABEL-SW  PIC X(1)          00012900
000096                                     VALUE     SPACE.             00013000
000097     88 OMGPR-JIT-ITEM-APPLY-LAB                                  00013100
000098                                     VALUE     'Y'.               00013200
000099              20 OMGPR-JIT-ITEM-BREAK-BULK-SW   PIC X(1)          00013300
000100                                     VALUE     SPACE.             00013400
000101     88 OMGPR-JIT-ITEM-BREAK-BULK                                 00013500
000102                                     VALUE     'Y'.               00013600
000103              20 OMGPR-ALT-ORD-DATA.                              00013700
000104              25 OMGPR-ALT-ORD-UM-FOUND-SW      PIC X(1)          00013800
000105                                     VALUE     SPACE.             00013900
000106     88 OMGPR-ALT-ORD-UM-FOUND                                    00014000
000107                                     VALUE     'Y'.               00014100
000108              25 OMGPR-ALT-ORD-UM               PIC X(3)          00014200
000109                                     VALUE     SPACE.             00014300
000110              25 OMGPR-ALT-ORD-CONV-FACTOR      PIC S9(7)V9(8)    00014400
000111                                     VALUE     ZERO               00014500
000112                                     COMPUTATIONAL-3.             00014600
000113              20 OMGPR-LABEL-UM-DATA.                             00014700
000114              25 OMGPR-LABEL-UM-FOUND-SW        PIC X(1)          00014800
000115                                     VALUE     SPACE.             00014900
000116                 88 OMGPR-LABEL-UM-FOUND                          00015000
000117                                     VALUE     'Y'.               00015100
000118              25 OMGPR-CUST-LABEL-UNIT          PIC X(3)          00015200
000119                                     VALUE     SPACE.             00015300
000120              25 OMGPR-CUST-LABEL-CONV-FACTOR   PIC S9(7)V9(8)    00015400
000121                                     VALUE     ZERO               00015500
000122                                     COMPUTATIONAL-3.             00015600
000127           15 OMGPR-DATA-FROM-CUP100.                             00016100
000128              20 OMGPR-F-AA-VHA-IND             PIC X(1)          00016200
000129                                     VALUE     SPACE.             00016300
000130              20 OMGPR-F-ST-JIT-CUSTOMER        PIC X(1)          00016400
000131                                     VALUE     SPACE.             00016500
000132              20 OMGPR-C-ACCT-ROUNDING          PIC X(1)          00016600
000133                                     VALUE     SPACE.             00016700
000134     88 OMGPR-NO-ROUNDING                                         00016800
000135                                     VALUE     'N'                00016900
000136     ' '                                                          00017000
000137     .                                                            00017100
000138     88 OMGPR-NORMAL-TWO-ROUNDING                                 00017200
000139                                     VALUE     'R'.               00017300
000140     88 OMGPR-UP-TWO-ROUNDING                                     00017400
000141                                     VALUE     'Y'.               00017500
000142              20 OMGPR-C-ACCT-PRCE-METHOD       PIC X(2)          00017600
000143                                     VALUE     SPACE.             00017700
000144                 88 OMGPR-CONT-PRCE-METHOD                        00017800
000145                                     VALUE     'CC'.              00017900
000146                 88 OMGPR-STOCK-PRCE-METHOD                       00018000
000147                                     VALUE     'ST'.              00018100
000148                 88 OMGPR-USAGE-PRCE-METHOD                       00018200
000149                                     VALUE     'AU'.              00018300
000150              20 OMGPR-F-AA-FRT-IN              PIC X(1)          00018400
000151                                     VALUE     SPACE.             00018500
000152              20 OMGPR-I-ACCOUNT                PIC S9(8)         00018600
000153                                     VALUE     ZERO               00018700
000154                                     COMPUTATIONAL.               00018800
000155              20 OMGPR-I-CUSTOMER               PIC S9(8)         00018900
000156                                     VALUE     ZERO               00019000
000157                                     COMPUTATIONAL.               00019100
000158              20 OMGPR-C-BUSINESS               PIC X(2)          00019200
000159                                     VALUE     SPACE.             00019300
NK0415*     2 FIELDS BELOW  ARE USED TO STORE GADJ FEES
NK0415              20 OMGPR-P-BREAK-BULK             PIC S9(1)V9(4)    00019400
NK0415                                     VALUE     ZERO               00019500
NK0415                                     COMPUTATIONAL-3.             00019600
000163              20 OMGPR-P-LOW-UOM                PIC S9(1)V9(4)    00019700
000164                                      USAGE COMP-3.               00019800
NK0415              20 OMGPR-F-LUOM-SW                PIC X(01).        00019900
NK0415                 88  OMGPR-F-LUOM-ACCOUNT       VALUE 'A'.
NK0415                 88  OMGPR-F-LUOM-GROUP         VALUE 'G'.
NK0415              20 OMGPR-F-BREAK-BULK-SW          PIC X(01).
NK0415                 88  OMGPR-F-BREAK-BULK-ACCOUNT VALUE 'A'.
NK0415                 88  OMGPR-F-BREAK-BULK-GROUP   VALUE 'G'.
NK0415              20 FILLER                         PIC X(01).        00020000
000008              20 OMGPR-F-LUOM-VEND-EXCL         PIC X(01).        00020100
000008              20 OMGPR-I-BUY-GROUP-LUOM      PIC S9(08) COMP.     00020200
000009              20 OMGPR-D-BG-LOW-UOM-EFF         PIC X(10).        00020300
SA0403              20 OMGPR-F-LUOM-ELIG-SW           PIC X(01).        00020400
SA0703                                                                  00020500
SA0703    15 OMGPR-DATA-FOR-PACKS.                                      00020600
SA0703              20 OMGPR-INPUT-PRODUCT-TYPE       PIC X(01).        00020700
SA0703                  88 OMGPR-INPUT-OMKIT             VALUE 'O'.     00020800
SA0703                  88 OMGPR-INPUT-VENDKIT           VALUE 'S'.     00020900
SA0703                  88 OMGPR-INPUT-REGULAR-PROD      VALUE 'R' .    00021000
SA0703                  88 OMGPR-INPUT-DEFAULT-PROD      VALUE ' '.     00021100
000166              20 OMGPR-PRICING-REQ-SW           PIC X(01).        00021200
SA0703                  88 OMGPR-PRICING-COSTONLY         VALUE 'C'.    00021300
SA0703                  88 OMGPR-PRICING-SELLONLY         VALUE 'S'.    00021400
SA0703                  88 OMGPR-PRICING-PRICE            VALUE 'P'.    00021500
000005*                    GET JIT CHARGES ON COST GIVEN IN TOT COST    00021600
SA0703                  88 OMGPR-JIT-ON-COST              VALUE 'J'.    00021700
SA0703                  88 OMGPR-PRICING-DEFAULT          VALUE ' '.    00021800
SA0703                                                                  00021900
SA0703              20 OMGPR-C-PRODUCT-TYPE           PIC X(1)          00022000
SA0703                                                  VALUE     SPACE.00022100
SA0703              20 OMGPR-A-OVERHEAD-FEE           PIC S9(5)V9(8)    00022200
SA0703                                                  VALUE     ZERO  00022300
SA0703                                                  COMPUTATIONAL-3.00022400
SA0703              20 OMGPR-A-THIRDPARTY-FEE         PIC S9(5)V9(8)    00022500
SA0703                                                   VALUE     ZERO 00022600
SA0703                                                  COMPUTATIONAL-3.00022700
SA0703              20 OMGPR-C-APLY-TRDPTY-BEFMKUP-SW PIC X(01)         00022800
SA0703                                                 VALUE     SPACES.00022900
SA0504           15 OMGPR-F-INP-CNT-EXCL-SW         PIC X(01)           00023000
SA0504                                     VALUE     SPACE.             00023100
SA0504              88 OMGPR-ACT-CNT-EXCL-EXISTS     VALUE 'Y'.         00023200
SA0504              88 OMGPR-ACT-CNT-EXCL-ABSENT     VALUE 'N' ' '.     00023300
KS0411*          15 OMGPR-T-INPUT-FILLER              PIC X(43)         00023400
KS0411*                                    VALUE     SPACE.             00023500
KS0411           15 OMGPR-F-EXEMPT-SANC-FLAG       PIC X(01).
KS0411           15 OMGPR-F-EXEMPT-NON-SANC-FLAG   PIC X(01).
KS0411           15 OMGPR-F-EXEMPT-IND-FLAG        PIC X(01).
KS0411           15 OMGPR-F-EXEMPT-NON-CONT-FLAG   PIC X(01).
KS0411           15 OMGPR-F-EXEMPT-CUSTOM-FLAG     PIC X(01).
BM0612*          15 OMGPR-T-INPUT-FILLER              PIC X(38)         00023400
BM0612*                                    VALUE     SPACE.             00023500
BM0612           15 OMGPR-VAR-I-BUY-GROUP          PIC S9(8)
BM0612                                     VALUE     ZERO
BM0612                                     COMPUTATIONAL.
BM0612*          15 OMGPR-T-INPUT-FILLER              PIC X(34)         00023400
BM0612*                                    VALUE     SPACE.             00023500
                 15    OMGPR-F-SPECIAL-CONTRACT       PIC X(01).        00023700
KS0912           15    OMGPR-JIT-LUM-FEE-PCT          PIC S9(1)V9(4)
KS0912                                     VALUE     ZERO
KS0912                                     COMPUTATIONAL-3.
KS0912           15    OMGPR-JIT-EXTRA-DELIV-FEE-PCT  PIC S9(1)V9(4)
KS0912                                     VALUE     ZERO
KS0912                                     COMPUTATIONAL-3.
NK0813           15    OMGPR-JIT-NON-OM-SLCT-FEE-PCT  PIC S9(1)V9(4)
NK0813                                     VALUE     ZERO
NK0813                                     COMPUTATIONAL-3.
KS0912*          15 OMGPR-T-INPUT-FILLER              PIC X(27)
KS0912*                                    VALUE     SPACE.
NK0813*          15 OMGPR-T-INPUT-FILLER              PIC X(16)
NK0813*                                    VALUE     SPACE.
BM0421           15 OMGPR-CUSTOMER-NBR                PIC S9(10)
BM0421                                     VALUE     ZERO
BM0421                                     COMPUTATIONAL-3.
DP0722           15 OMGPR-ORDER-TYPE       PIC X(1)    VALUE SPACE.
SJ0722              88 OMGPR-ORDER-TYPE-STOCK VALUE 'S' ' '.
     **          * SURGITRAK                     
SJ0722           15 OMGPR-A-SURGITRAK-ST   PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
SJ0722           15 OMGPR-P-SURGITRAK-ST   PIC S9(3)V9(4)            
SJ0722                                                 VALUE 0 COMP-3.
SJ0722           15 OMGPR-FEE-SHRT-CODE-ST PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-ST      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-ST   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-ST             VALUE 'DL'.
SJ0722              88 OMGPR-DAILY-BILL-SEP-ST         VALUE 'DS'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-ST      VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-ST    VALUE 'MM'.
SJ0722           15 OMGPR-FEE-TYPE-ST      PIC X(01)   VALUE SPACE.
SJ0722              88 OMGPR-FEE-PER-LINE-ST        VALUE 'L'.
SJ0722              88 OMGPR-FEE-PER-QTY-ST         VALUE 'Q'.
SJ0722              88 OMGPR-FEE-PER-ORDER-ST       VALUE 'O'.
SJ0722              88 OMGPR-FEE-PCT-SELL-ST        VALUE 'P'.
SJ0722              88 OMGPR-FEE-PCT-COST-ST        VALUE 'C'.
     **          * BREAKBULK, LUM                          
SJ0722           15 OMGPR-A-BREAKBULK-BB   PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
SJ0722           15 OMGPR-P-BREAKBULK-BB   PIC S9(3)V9(4)            
SJ0722                                                 VALUE 0 COMP-3.
SJ0722           15 OMGPR-FEE-SHRT-CODE-BB PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-BB      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-BB   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-BB          VALUE 'DL'.
SJ0722              88 OMGPR-DAILY-BILL-SEP-BB      VALUE 'DS'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-BB   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-BB VALUE 'MM'.
SJ0722           15 OMGPR-FEE-TYPE-BB      PIC X(01)   VALUE SPACE.
SJ0722              88 OMGPR-FEE-PER-LINE-BB        VALUE 'L'.
SJ0722              88 OMGPR-FEE-PER-QTY-BB         VALUE 'Q'.
SJ0722              88 OMGPR-FEE-PER-ORDER-BB       VALUE 'O'.
SJ0722              88 OMGPR-FEE-PCT-SELL-BB        VALUE 'P'.
SJ0722              88 OMGPR-FEE-PCT-COST-BB        VALUE 'C'.
SJ0722           15 OMGPR-A-LUM-FEE-AMT-BB PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
SJ0722           15 OMGPR-P-LUM-FEE-PCT-BB PIC S9(3)V9(4)            
SJ0722                                                 VALUE 0 COMP-3.
     **          * APPLY LABEL                       
SJ0722           15 OMGPR-A-APPLY-LAB-AL   PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
SJ0722           15 OMGPR-P-APPLY-LAB-AL   PIC S9(3)V9(4)             
SJ0722                                                 VALUE 0 COMP-3.
SJ0722           15 OMGPR-FEE-SHRT-CODE-AL PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-AL      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-AL   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-AL          VALUE 'DL'.
SJ0722              88 OMGPR-DAILY-BILL-SEP-AL      VALUE 'DS'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-AL   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-AL VALUE 'MM'.
SJ0722           15 OMGPR-FEE-TYPE-AL      PIC X(01)   VALUE SPACE.
SJ0722              88 OMGPR-FEE-PER-LINE-AL        VALUE 'L'.
SJ0722              88 OMGPR-FEE-PER-QTY-AL         VALUE 'Q'.
SJ0722              88 OMGPR-FEE-PER-ORDER-AL       VALUE 'O'.
SJ0722              88 OMGPR-FEE-PCT-SELL-AL        VALUE 'P'.
SJ0722              88 OMGPR-FEE-PCT-COST-AL        VALUE 'C'.
     **          * STANDARD FREIGHT                   
SJ0722           15 OMGPR-FEE-SHRT-CODE-SF PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-SF      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-SF   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-SF          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-SF   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-SF VALUE 'MM'.
     **          * STANDARD TERMS AND SURCHARGES              
SJ0722           15 OMGPR-FEE-SHRT-CODE-TS PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-TS      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-TS   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-TS          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-TS   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-TS VALUE 'MM'.
     **          * STANDARD DIST MARKUP GROUP SANCTION
SJ0722           15 OMGPR-FEE-SHRT-CODE-GS PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-GS      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-GS   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-GS          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-GS   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-GS VALUE 'MM'.
     **          * STANDARD DIST MARKUP GROUP NON-SANCTION
SJ0722           15 OMGPR-FEE-SHRT-CODE-GN PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-GN      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-GN   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-GN          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-GN   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-GN VALUE 'MM'.
     **          * STANDARD DIST MARKUP INDIVIDUAL    
SJ0722           15 OMGPR-FEE-SHRT-CODE-MI PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-MI      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-MI   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-MI          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-MI   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-MI VALUE 'MM'.
     **          * STANDARD DIST MARKUP NON-CONTRACT
SJ0722           15 OMGPR-FEE-SHRT-CODE-MN PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-MN      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-MN   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-MN          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-MN   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-MN VALUE 'MM'.
     **          * STANDARD DIST MARKUP CUSTOM             
SJ0722           15 OMGPR-FEE-SHRT-CODE-MC PIC X(02)   VALUE SPACE.
SJ0722           15 OMGPR-SKU-CODE-MC      PIC X(04)   VALUE SPACE.
SJ0722           15 OMGPR-BILLING-FRQ-MC   PIC X(02)   VALUE SPACE.
SJ0722              88 OMGPR-DAILY-BILL-MC          VALUE 'DL'.
SJ0722              88 OMGPR-MONTHLY-AUTO-BILL-MC   VALUE 'MA'.
SJ0722              88 OMGPR-MONTHLY-MANUAL-BILL-MC VALUE 'MM'.
SJ0722           15 FILLER                 PIC X(09) VALUE SPACES.
000168        10 OMGPR-PRC-OUTPUT-FIELDS.                               00023800
000169           15 OMGPR-ERROR-FIELDS.                                 00023900
000170              20 OMGPR-F-PRICER-ERROR           PIC X(1)          00024000
000171                                     VALUE     SPACE.             00024100
000172                 88 OMGPR-PRICER-ERROR                            00024200
000173                                     VALUE     'Y'.               00024300
000174              20 OMGPR-ERROR-MESSAGE            PIC X(76)         00024400
000175                                     VALUE     SPACE.             00024500
000176           15 OMGPR-PRODUCT-DATA.                                 00024600
000177              20 OMGPR-C-VND-PROD-BASE-UM       PIC X(2)          00024700
000178                                     VALUE     SPACE.             00024800
000179              20 OMGPR-A-VD-PRD-ALT-UMF         PIC S9(6)V9(8)    00024900
000180                                     VALUE     ZERO               00025000
000181                                     COMPUTATIONAL-3.             00025100
000182              20 OMGPR-S-PROD-CATEGORY          PIC S9(8)         00025200
000183                                     VALUE     ZERO               00025300
000184                                     COMPUTATIONAL.               00025400
000185              20 OMGPR-C-INV-CLASS              PIC X(1)          00025500
000186                                     VALUE     SPACE.             00025600
NK0408              20 OMGPR-C-CUSTOM-IND            PIC X(01)          00025610
NK0408                                     VALUE     SPACE.             00025611
NK0408              20 OMGPR-C-PRIVATE-LBL           PIC X(01)          00025620
NK0408                                     VALUE     SPACE.             00025630
NK0408              20 OMGPR-C-SUPPLIER-NAME         PIC X(35)          00025640
NK0408                                     VALUE     SPACE.             00025641
NK0408              20 OMGPR-C-SUPPLIER-NUMBER       PIC X(04)          00025650
NK0408                                     VALUE     SPACE.             00025651
NK0408              20 OMGPR-C-VND-PROD-DESC-1       PIC X(35)          00025660
NK0408                                     VALUE     SPACE.             00025661
NK0408              20 OMGPR-C-VND-PROD-DESC-2       PIC X(35)          00025670
NK0408                                     VALUE     SPACE.             00025671
NK0408              20 OMGPR-C-VND-CATALOG-NBR       PIC X(12)          00025680
NK0408                                     VALUE     SPACE.             00025690
000187           15 OMGPR-COST-CONT-DATA.                               00025700
000188              20 OMGPR-I-CONTRACT               PIC S9(8)         00025800
000189                                     VALUE     ZERO               00025900
000190                                     COMPUTATIONAL.               00026000
000191              20 OMGPR-C-CNT-ENTRY-METHOD       PIC X(2)          00026100
000192                                     VALUE     SPACE.             00026200
000194                 88 OMGPR-CNT-ENTRY-COST-DISC    VALUE     '07'.  00026400
000196                 88 OMGPR-CNT-ENTRY-FIXED-REB    VALUE     '06'.  00026600
000198                 88 OMGPR-CNT-ENTRY-SS-BROKER    VALUE     '02'.  00026800
000200                 88 OMGPR-CNT-ENTRY-SS-COST-D    VALUE     '05'.  00027000
000202                 88 OMGPR-CNT-ENTRY-SS-COST-P    VALUE     '03'.  00027200
000204                 88 OMGPR-CNT-ENTRY-SS-STATED    VALUE     '04'.  00027400
000206                 88 OMGPR-CNT-ENTRY-STATED-CO    VALUE     '01'.  00027600
000207              20 OMGPR-D-CNT-PROT-START         PIC X(10)         00027700
000208                                     VALUE     SPACE.             00027800
000209              20 OMGPR-D-CNT-PROT-END           PIC X(10)         00027900
000210                                     VALUE     SPACE.             00028000
000211              20 OMGPR-T-COMMENT                PIC X(35)         00028100
000212                                     VALUE     SPACE.             00028200
000213              20 OMGPR-I-CNT-VEND               PIC X(20)         00028300
000214                                     VALUE     SPACE.             00028400
000215              20 OMGPR-I-BUY-GROUP              PIC S9(8)         00028500
000216                                     VALUE     ZERO               00028600
000217                                     COMPUTATIONAL.               00028700
000218              20 OMGPR-S-BG-MEMBER              PIC S9(8)         00028800
000219                                     VALUE     ZERO               00028900
000220                                     COMPUTATIONAL.               00029000
000221              20 OMGPR-N-BUY-GROUP-SHORT        PIC X(8)          00029100
000222                                     VALUE     SPACE.             00029200
000223              20 OMGPR-F-GRP-CNT-FEES           PIC X(1)          00029300
000224                                     VALUE     SPACE.             00029400
000225              20 OMGPR-C-BG-TYPE                PIC X(1)          00029500
000226                                     VALUE     SPACE.             00029600
NK0408              20 OMGPR-C-PROD-CATGRY         PIC X(35)            00029602
NK0408                                     VALUE     SPACE.             00029603
NK0408              20 OMGPR-C-REPLACES-CNT        PIC X(20)            00029604
NK0408                                     VALUE     SPACE.             00029605
NK0408              20 OMGPR-D-RCVD-DATE           PIC X(10)            00029606
NK0408                                     VALUE     SPACE.             00029607
NK0408              20 OMGPR-C-EXPIRE-MSG          PIC X(35)            00029608
NK0408                                     VALUE     SPACE.             00029609
NK0408              20 OMGPR-C-TIER                PIC X(03)            00029610
NK0408                                     VALUE     SPACE.             00029611
NK0408              20 OMGPR-C-REPLACED-BY-CNT     PIC X(20)            00029612
NK0408                                     VALUE     SPACE.             00029613
NK0408              20 OMGPR-I-RPT-GRP             PIC S9(8)            00029614
NK0408                                     VALUE     ZERO               00029615
NK0408                                     COMPUTATIONAL.               00029616
NK0408              20 OMGPR-I-RPT-GRP-CNT         PIC X(20)            00029620
NK0408                                     VALUE     SPACE.             00029630
000227              20 OMGPR-L-CNT-LINE               PIC S9(7)         00029700
000228                                     VALUE     ZERO               00029800
000229                                     COMPUTATIONAL.               00029900
000230              20 OMGPR-C-CNT-LN-UM              PIC X(2)          00030000
000231                                     VALUE     SPACE.             00030100
000232              20 OMGPR-A-CNT-LN-UNIT-COST       PIC S9(5)V9(8)    00030200
000233                                     VALUE     ZERO               00030300
000234                                     COMPUTATIONAL-3.             00030400
000235              20 OMGPR-A-CNT-LN-SUGG-SELL       PIC S9(5)V9(8)    00030500
000236                                     VALUE     ZERO               00030600
000237                                     COMPUTATIONAL-3.             00030700
000238              20 OMGPR-P-CNT-LN-COST-PLUS       PIC S9(1)V9(4)    00030800
000239                                     VALUE     ZERO               00030900
000240                                     COMPUTATIONAL-3.             00031000
000241              20 OMGPR-P-CNT-LN-COST-DISC       PIC S9(1)V9(4)    00031100
000242                                     VALUE     ZERO               00031200
000243                                     COMPUTATIONAL-3.             00031300
000244              20 OMGPR-P-CNT-LN-BROKER          PIC S9(1)V9(4)    00031400
000245                                     VALUE     ZERO               00031500
000246                                     COMPUTATIONAL-3.             00031600
000247              20 OMGPR-P-CNT-LN-ACQ-MKD         PIC S9(1)V9(4)    00031700
000248                                     VALUE     ZERO               00031800
000249                                     COMPUTATIONAL-3.             00031900
000250              20 OMGPR-A-CNT-LN-PROT-ACQ        PIC S9(5)V9(8)    00032000
000251                                     VALUE     ZERO               00032100
000252                                     COMPUTATIONAL-3.             00032200
000253              20 OMGPR-A-CNT-LN-MAX-REBT        PIC S9(5)V9(8)    00032300
000254                                     VALUE     ZERO               00032400
000255                                     COMPUTATIONAL-3.             00032500
000256              20 OMGPR-C-CNT-TYPE               PIC X(1)          00032600
000257                                     VALUE     SPACE.             00032700
000258           15 OMGPR-PRICE-LIST-DATA.                              00032800
000259              20 OMGPR-C-VND-PRC-LEVEL          PIC X(2)          00032900
000260                                     VALUE     SPACE.             00033000
000261              20 OMGPR-D-VND-PRC-LIST-EFF       PIC X(10)         00033100
000262                                     VALUE     SPACE.             00033200
000263              20 OMGPR-C-VND-PRC-UM             PIC X(2)          00033300
000264                                     VALUE     SPACE.             00033400
000265              20 OMGPR-A-VND-PRC-DEALER         PIC S9(7)V9(8)    00033500
000266                                     VALUE     ZERO               00033600
000267                                     COMPUTATIONAL-3.             00033700
000268              20 OMGPR-A-VND-PRC-ACQ-COST       PIC S9(7)V9(8)    00033800
000269                                     VALUE     ZERO               00033900
000270                                     COMPUTATIONAL-3.             00034000
000271              20 OMGPR-A-VND-PRC-BEST-QTY       PIC S9(7)V9(8)    00034100
000272                                     VALUE     ZERO               00034200
000273                                     COMPUTATIONAL-3.             00034300
000274              20 OMGPR-A-VND-PRC-LST-HOSP       PIC S9(7)V9(8)    00034400
000275                                     VALUE     ZERO               00034500
000276                                     COMPUTATIONAL-3.             00034600
000277              20 OMGPR-A-VND-PRC-LST-DOC        PIC S9(7)V9(8)    00034700
000278                                     VALUE     ZERO               00034800
000279                                     COMPUTATIONAL-3.             00034900
000280              20 OMGPR-F-VEND-BST-CST-RBT       PIC X(1)          00035000
000281                                     VALUE     SPACE.             00035100
000282              20 OMGPR-F-VEND-NET-CST-RBT       PIC X(1)          00035200
000283                                     VALUE     SPACE.             00035300
000284           15 OMGPR-UNIT-REB-AMOUNT.                              00035400
000285              20 OMGPR-A-CUR-SELL-CST-DIF       PIC S9(5)V9(4)    00035500
000286                                     VALUE     ZERO               00035600
000287                                     COMPUTATIONAL-3.             00035700
000288              20 OMGPR-A-CUR-VND-DIST-REB       PIC S9(5)V9(4)    00035800
000289                                     VALUE     ZERO               00035900
000290                                     COMPUTATIONAL-3.             00036000
000291              20 OMGPR-A-CUR-TOTAL-REBATE       PIC S9(5)V9(4)    00036100
000292                                     VALUE     ZERO               00036200
000293                                     COMPUTATIONAL-3.             00036300
000294              20 OMGPR-A-CUR-ADD-DISC-REB       PIC S9(5)V9(4)    00036400
000295                                     VALUE     ZERO               00036500
000296                                     COMPUTATIONAL-3.             00036600
000297              20 OMGPR-A-CUR-PRICE-PROT         PIC S9(5)V9(4)    00036700
000298                                     VALUE     ZERO               00036800
000299                                     COMPUTATIONAL-3.             00036900
000300              20 OMGPR-A-CUR-CONT-REBATE        PIC S9(5)V9(4)    00037000
000301                                     VALUE     ZERO               00037100
000302                                     COMPUTATIONAL-3.             00037200
000303              20 OMGPR-A-REBATE-COST            PIC S9(5)V9(4)    00037300
000304                                     VALUE     ZERO               00037400
000305                                     COMPUTATIONAL-3.             00037500
000306           15 OMGPR-UNIT-SELL-AMT.                                00037600
000307              20 OMGPR-C-CUS-SELL-PRC-UOM       PIC X(2)          00037700
000308                                     VALUE     SPACE.             00037800
000309              20 OMGPR-A-CUS-UOM-SELL-PRC       PIC S9(5)V9(8)    00037900
000310                                     VALUE     ZERO               00038000
000311                                     COMPUTATIONAL-3.             00038100
000312              20 OMGPR-A-TOTAL-COST             PIC S9(5)V9(8)    00038200
000313                                     VALUE     ZERO               00038300
000314                                     COMPUTATIONAL-3.             00038400
000315              20 OMGPR-A-TOTAL-SELL             PIC S9(5)V9(8)    00038500
000316                                     VALUE     ZERO               00038600
000317                                     COMPUTATIONAL-3.             00038700
000318              20 OMGPR-A-TOTAL-ADJ-COST         PIC S9(5)V9(8)    00038800
000319                                     VALUE     ZERO               00038900
000320                                     COMPUTATIONAL-3.             00039000
000321              20 OMGPR-A-TOTAL-ADJ-SELL         PIC S9(5)V9(8)    00039100
000322                                     VALUE     ZERO               00039200
000323                                     COMPUTATIONAL-3.             00039300
000324           15 OMGPR-SELL-ARR.                                     00039400
000325              20 OMGPR-I-BAS-SELL               PIC S9(8)         00039500
000326                                     VALUE     ZERO               00039600
000327                                     COMPUTATIONAL.               00039700
000328              20 OMGPR-D-BAS-SELL-EFF           PIC X(10)         00039800
000329                                     VALUE     SPACE.             00039900
000330              20 OMGPR-PRICING-METHOD           PIC X(10)         00040000
000331                                     VALUE     SPACE.             00040100
000332              20 OMGPR-PRICING-PERCENTAGE       PIC 9(1)V9(4)     00040200
000333                                     VALUE     ZERO               00040300
000334                                     COMPUTATIONAL-3.             00040400
000335              20 OMGPR-C-SELL-PRC-METHOD        PIC X(1)          00040500
000336                                     VALUE     SPACE.             00040600
000337              20 OMGPR-T-SELL-COMMENT           PIC X(35)         00040700
000338                                     VALUE     SPACE.             00040800
000339              20 OMGPR-Q-PREF-TIER-LEVEL        PIC S9(4)         00040900
000340                                     VALUE     ZERO               00041000
000341                                     COMPUTATIONAL.               00041100
000342              20 OMGPR-C-SELL-TYPE              PIC X(1)          00041200
000343                                     VALUE     SPACE.             00041300
000344              20 OMGPR-C-SELL-LEVEL             PIC X(1)          00041400
000345                                     VALUE     SPACE.             00041500
000346              20 OMGPR-I-BUY-GROUP-SELL         PIC S9(8)         00041600
000347                                     VALUE     ZERO               00041700
000348                                     COMPUTATIONAL.               00041800
000349              20 OMGPR-S-BG-MEMBER-SELL         PIC S9(8)         00041900
000350                                     VALUE     ZERO               00042000
000351                                     COMPUTATIONAL.               00042100
000352              20 OMGPR-N-BUY-GROUP-SHORT-SELL   PIC X(8)          00042200
000353                                     VALUE     SPACE.             00042300
000354              20 OMGPR-C-BG-TYPE-SELL           PIC X(1)          00042400
000355                                     VALUE     SPACE.             00042500
NK0615              20 OMGPR-C-SELLGROUP-INFO         PIC X(19)
NK0615                                     VALUE     SPACE.
000356           15 OMGPR-COST-ADJUSTMENTS.                             00042600
000357              20 OMGPR-A-OVERHEAD-CHG           PIC S9(7)V9(8)    00042700
000358                                     VALUE     ZERO               00042800
000359                                     COMPUTATIONAL-3.             00042900
000360              20 OMGPR-P-OVERHEAD-CHG           PIC S9(1)V9(4)    00043000
000361                                     VALUE     ZERO               00043100
000362                                     COMPUTATIONAL-3.             00043200
000363              20 OMGPR-A-CUST-INV-CLASS-CT      PIC S9(7)V9(8)    00043300
000364                                     VALUE     ZERO               00043400
000365                                     COMPUTATIONAL-3.             00043500
000366              20 OMGPR-P-CUST-INV-CLASS-CT      PIC S9(1)V9(4)    00043600
000367                                     VALUE     ZERO               00043700
000368                                     COMPUTATIONAL-3.             00043800
000369              20 OMGPR-C-INV-CLASS-CT           PIC X(1)          00043900
000370                                     VALUE     SPACE.             00044000
000371              20 OMGPR-C-INFRT-TYPE             PIC X(1)          00044100
000372                                     VALUE     SPACE.             00044200
000373              20 OMGPR-I-BUY-GROUP-FRT          PIC S9(8)         00044300
000374                                     VALUE     ZERO               00044400
000375                                     COMPUTATIONAL.               00044500
000376              20 OMGPR-A-INFRT                  PIC S9(7)V9(8)    00044600
000377                                     VALUE     ZERO               00044700
000378                                     COMPUTATIONAL-3.             00044800
000379              20 OMGPR-P-INFRT-BG               PIC S9(1)V9(4)    00044900
000380                                     VALUE     ZERO               00045000
000381                                     COMPUTATIONAL-3.             00045100
000382           15 OMGPR-SELL-ADJUSTMENTS.                             00045200
000383              20 OMGPR-A-CUST-INV-CLASS-SL      PIC S9(7)V9(8)    00045300
000384                                     VALUE     ZERO               00045400
000385                                     COMPUTATIONAL-3.             00045500
000386              20 OMGPR-P-CUST-INV-CLASS-SL      PIC S9(1)V9(4)    00045600
000387                                     VALUE     ZERO               00045700
000388                                     COMPUTATIONAL-3.             00045800
000389              20 OMGPR-C-INV-CLASS-SL           PIC X(1)          00045900
000390                                     VALUE     SPACE.             00046000
000391              20 OMGPR-I-BUY-GROUP-CNT          PIC S9(8)         00046100
000392                                     VALUE     ZERO               00046200
000393                                     COMPUTATIONAL.               00046300
000394              20 OMGPR-A-NONCONT-GRP            PIC S9(7)V9(8)    00046400
000395                                     VALUE     ZERO               00046500
000396                                     COMPUTATIONAL-3.             00046600
000397              20 OMGPR-P-NONCONT-GRP            PIC S9(1)V9(4)    00046700
000398                                     VALUE     ZERO               00046800
000399                                     COMPUTATIONAL-3.             00046900
000400              20 OMGPR-A-RISK-PREMIUM           PIC S9(7)V9(8)    00047000
000401                                     VALUE     ZERO               00047100
000402                                     COMPUTATIONAL-3.             00047200
000403              20 OMGPR-P-RISK-PREMIUM           PIC S9(1)V9(4)    00047300
000404                                     VALUE     ZERO               00047400
000405                                     COMPUTATIONAL-3.             00047500
000406              20 OMGPR-A-DELIVERY-ADJ           PIC S9(7)V9(8)    00047600
000407                                     VALUE     ZERO               00047700
000408                                     COMPUTATIONAL-3.             00047800
000409              20 OMGPR-P-DELIVERY-ADJ           PIC S9(1)V9(4)    00047900
000410                                     VALUE     ZERO               00048000
000411                                     COMPUTATIONAL-3.             00048100
000412              20 OMGPR-A-FINANCE-CHG            PIC S9(7)V9(8)    00048200
000413                                     VALUE     ZERO               00048300
000414                                     COMPUTATIONAL-3.             00048400
000415              20 OMGPR-P-FINANCE-CHG            PIC S9(1)V9(4)    00048500
000416                                     VALUE     ZERO               00048600
000417                                     COMPUTATIONAL-3.             00048700
000418              20 OMGPR-A-NONCONT-CUST           PIC S9(7)V9(8)    00048800
000419                                     VALUE     ZERO               00048900
000420                                     COMPUTATIONAL-3.             00049000
000421              20 OMGPR-P-NONCONT-CUST           PIC S9(1)V9(4)    00049100
000422                                     VALUE     ZERO               00049200
000423                                     COMPUTATIONAL-3.             00049300
000424              20 OMGPR-A-PREPAY-DEDUCTION       PIC S9(7)V9(8)    00049400
000425                                     VALUE     ZERO               00049500
000426                                     COMPUTATIONAL-3.             00049600
000427              20 OMGPR-P-PREPAY-DEDUCTION       PIC S9(1)V9(4)    00049700
000428                                     VALUE     ZERO               00049800
000429                                     COMPUTATIONAL-3.             00049900
000430              20 OMGPR-A-VOLUME-DISCOUNT        PIC S9(7)V9(8)    00050000
000431                                     VALUE     ZERO               00050100
000432                                     COMPUTATIONAL-3.             00050200
000433              20 OMGPR-P-VOLUME-DISCOUNT        PIC S9(1)V9(4)    00050300
000434                                     VALUE     ZERO               00050400
000435                                     COMPUTATIONAL-3.             00050500
000436              20 OMGPR-I-BUY-GROUP-VOL          PIC S9(8)         00050600
000437                                     VALUE     ZERO               00050700
000438                                     COMPUTATIONAL.               00050800
000439              20 OMGPR-A-BG-VOL-DISC            PIC S9(7)V9(8)    00050900
000440                                     VALUE     ZERO               00051000
000441                                     COMPUTATIONAL-3.             00051100
000442              20 OMGPR-P-BG-VOL-DISC            PIC S9(1)V9(4)    00051200
000443                                     VALUE     ZERO               00051300
000444                                     COMPUTATIONAL-3.             00051400
000445              20 OMGPR-A-PANDAC                 PIC S9(7)V9(8)    00051500
000446                                     VALUE     ZERO               00051600
000447                                     COMPUTATIONAL-3.             00051700
000448              20 OMGPR-P-PANDAC-ADJ             PIC S9(1)V9(4)    00051800
000449                                     VALUE     ZERO               00051900
000450                                     COMPUTATIONAL-3.             00052000
000451           15 OMGPR-JIT-ADJUSTMENTS.                              00052100
000452              20 OMGPR-A-JIT-SERVICE-FEE        PIC S9(7)V9(8)    00052200
000453                                     VALUE     ZERO               00052300
000454                                     COMPUTATIONAL-3.             00052400
000455              20 OMGPR-A-JIT-LABEL-FEE          PIC S9(7)V9(8)    00052500
000456                                     VALUE     ZERO               00052600
000457                                     COMPUTATIONAL-3.             00052700
000458              20 OMGPR-A-JIT-APPLY-FEE          PIC S9(7)V9(8)    00052800
000459                                     VALUE     ZERO               00052900
000460                                     COMPUTATIONAL-3.             00053000
000461              20 OMGPR-A-JIT-BREAK-FEE          PIC S9(7)V9(8)    00053100
000462                                     VALUE     ZERO               00053200
000463                                     COMPUTATIONAL-3.             00053300
000464              20 OMGPR-A-JIT-COST               PIC S9(7)V9(8)    00053400
000465                                     VALUE     ZERO               00053500
000466                                     COMPUTATIONAL-3.             00053600
000467              20 OMGPR-A-JIT-SELL               PIC S9(7)V9(8)    00053700
000468                                     VALUE     ZERO               00053800
000469                                     COMPUTATIONAL-3.             00053900
000470           15 OMGPR-ADDITIONAL-INFORMAT.                          00054000
000471              20 OMGPR-F-STOCK-ITEM             PIC X(1)          00054100
000472                                     VALUE     SPACE.             00054200
000473              20 OMGPR-D-EXPIRATION             PIC X(10)         00054300
000474                                     VALUE     SPACE.             00054400
000475              20 OMGPR-F-JIT-REMOVED            PIC X(1)          00054500
000476                                     VALUE     SPACE.             00054600
000477              20 OMGPR-I-GRP-CNT-NBR            PIC X(20)         00054700
000478                                     VALUE     SPACE.             00054800
000479              20 OMGPR-I-DIVISION-SELL          PIC X(2)          00054900
000480                                     VALUE     SPACE.             00055000
000481              20 OMGPR-A-CUS-UOM-SELL-PRC-UN    PIC S9(5)V9(8)    00055100
000482                                     VALUE     ZERO               00055200
000483                                     COMPUTATIONAL-3.             00055300
000484              20 OMGPR-A-TOTAL-SELL-UN          PIC S9(5)V9(8)    00055400
000485                                     VALUE     ZERO               00055500
000486                                     COMPUTATIONAL-3.             00055600
000487              20 OMGPR-I-DIVISION-COST          PIC X(2)          00055700
000488                                     VALUE     SPACE.             00055800
000489              20 OMGPR-F-JIT-EXEMPT             PIC X(1)          00055900
000490                                     VALUE     SPACE.             00056000
000491              20 OMGPR-F-FRT-EXEMPT             PIC X(1)          00056100
000492                                     VALUE     SPACE.             00056200
000493              20 OMGPR-Q-ERROR-NBR              PIC S9(4)         00056300
000494                                     VALUE     ZERO               00056400
000495                                     COMPUTATIONAL.               00056500
000496              20 OMGPR-Q-ERROR-CODE-X.                            00056600
000497              21 OMGPR-Q-ERROR-CODE             PIC 9(3)          00056700
000498                                     VALUE     ZERO.              00056800
000499                 88 OMGPR-ACCT-NBR-MISSING                        00056900
000500                                     VALUE     9.                 00057000
000501                 88 OMGPR-ACCT-NOT-FOUND                          00057100
000502                                     VALUE     2.                 00057200
000503                 88 OMGPR-ACT-CUST-NOT-FOUND                      00057300
000504                                     VALUE     3.                 00057400
000505                 88 OMGPR-BG-PRC-PARM-NOT-FND                     00057500
000506                                     VALUE     41.                00057600
000507                 88 OMGPR-DB2-FATAL-ERROR                         00057700
000508                                     VALUE     70.                00057800
000509                 88 OMGPR-DIV-NBR-MISSING                         00057900
000510                                     VALUE     8.                 00058000
000511                 88 OMGPR-PRC-LIST-NOT-FOUND                      00058100
000512                                     VALUE     4.                 00058200
000513                 88 OMGPR-PRC-METHOD-INVALID                      00058300
000514                                     VALUE     40.                00058400
000515                 88 OMGPR-PRICER-FATAL-ERROR                      00058500
000516                                     VALUE     70 THRU 99.        00058600
000519                 88 OMGPR-PRICER-MEDIUM-ERROR                     00058900
000520                                     VALUE     40 THRU 69.        00059000
000523                 88 OMGPR-PRICER-NO-ERROR                         00059300
000524                                     VALUE     0.                 00059400
000525                 88 OMGPR-PRICER-WARN-ERROR                       00059500
000527                                     VALUE     1 THRU 39.         00059700
000529                 88 OMGPR-PROD-NBR-MISSING                        00059900
000530                                     VALUE     11.                00060000
000531                 88 OMGPR-PROD-NOT-FOUND                          00060100
000532                                     VALUE     6.                 00060200
000533                  88 OMGPR-SHIP-TO-NOT-FOUND                      00060300
000534                                     VALUE     5.                 00060400
000535                  88 OMGPR-UM-NOT-FOUND                           00060500
000536                                     VALUE     1.                 00060600
000537                  88 OMGPR-VEND-NBR-MISSING                       00060700
000538                                     VALUE     10.                00060800
000539                  88 OMGPR-VENDOR-NOT-FOUND                       00060900
000540                                     VALUE     7.                 00061000
000541              20 OMGPR-F-PRICE-LOCKED           PIC X(1)          00061100
000542                                     VALUE     SPACE.             00061200
000543                  88 OMGPR-PRICE-LOCKED                           00061300
000544                                     VALUE     'Y'.               00061400
000545                  88 OMGPR-PRICE-NOT-LOCKED                       00061500
000546                                     VALUE     'N'.               00061600
000547              20 OMGPR-D-BG-TIER-START          PIC X(10)         00061700
000548                                     VALUE     SPACE.             00061800
000549              20 OMGPR-P-SURCHARGE              PIC S9(1)V9(4)    00061900
000550                                     VALUE     ZERO               00062000
000551                                     COMPUTATIONAL-3.             00062100
000552              20 OMGPR-A-SURCHARGE              PIC S9(5)V9(8)    00062200
000553                                     VALUE     ZERO               00062300
000554                                     COMPUTATIONAL-3.             00062400
000555              20 OMGPR-C-SURCHARGE              PIC X(2)          00062500
000556                                     VALUE     SPACE.             00062600
000557              20 OMGPR-C-VEND-VHA-PLUS          PIC X(1)          00062700
000558                                     VALUE     SPACE.             00062800
000559              20 OMGPR-Q-COST-PRIORITY          PIC S9(4)         00062900
000560                                     VALUE     ZERO               00063000
000561                                     COMPUTATIONAL.               00063100
000562              20 OMGPR-Q-SELL-PRIORITY          PIC S9(4)         00063200
000563                                     VALUE     ZERO               00063300
000564                                     COMPUTATIONAL.               00063400
000565              20 OMGPR-I-GRP-CONTROL-NBR        PIC X(20)         00063500
000566                                     VALUE     SPACE.             00063600

000549              20 OMGPR-P-VNDCOSTADJ             PIC S9(1)V9(4)    00064000
000550                                     VALUE     ZERO               00064100
000551                                     COMPUTATIONAL-3.             00064200
000552              20 OMGPR-A-VNDCOSTADJ             PIC S9(5)V9(8)    00064300
000553                                     VALUE     ZERO               00064400
000554                                     COMPUTATIONAL-3.             00064500
000555              20 OMGPR-C-VNDCOSTADJ             PIC X(2)          00064600
000556                                     VALUE     SPACE.             00064700
SA0504              20 OMGPR-F-CONT-EXCL-SW           PIC X(1)          00064800
SA0504                                     VALUE     SPACE.             00064900
SA0504                  88 OMGPR-CONTRACT-EXCLUDED     VALUE 'Y'.       00065000
SA0504                  88 OMGPR-CONTRACT-NOT-EXCLUDED VALUE 'N'.       00065100
NK0408              20 OMGPR-C-SOURCE             PIC X(20)             00065120
NK0408                                     VALUE     SPACE.             00065130
BN1211              20 OMGPR-VND-JIT-BB-SW        PIC X(1)
BN1211                                     VALUE     SPACE.
BN1211                  88 OMGPR-VND-JIT-BB-FEE        VALUE 'B'.
BN1211              20 OMGPR-VND-JIT-LB-SW        PIC X(1)
BN1211                                     VALUE     SPACE.
BN1211                  88 OMGPR-VND-JIT-LB-FEE        VALUE 'L'.
BN1211              20  OMGPR-VND-JIT-AB-SW       PIC X(1)
BN1211                                     VALUE     SPACE.
BN1211                  88 OMGPR-VND-JIT-AL-FEE        VALUE 'A'.
BN1211              20  OMGPR-VND-JIT-SF-SW       PIC X(1)
BN1211                                     VALUE     SPACE.
BN1211                  88 OMGPR-VND-JIT-SF-FEE        VALUE 'S'.
BN1211*          15 OMGPR-T-OUTPUT-FILLER             PIC X(19)
BN1211*          15 OMGPR-T-OUTPUT-FILLER             PIC X(15)
000580*                                    VALUE     SPACE.
KS0912           20 OMGPR-A-JIT-LUM-FEE            PIC S9(7)V9(8)
KS0912                                     VALUE     ZERO
KS0912                                     COMPUTATIONAL-3.
KS0912           20 OMGPR-A-JIT-EXTRA-DELIV-FEE    PIC S9(7)V9(8)
KS0912                                     VALUE     ZERO
KS0912                                     COMPUTATIONAL-3.
NK0813           20 OMGPR-A-JIT-NON-OM-SLCT-FEE    PIC S9(7)V9(8)
NK0813                                     VALUE     ZERO
NK0813                                     COMPUTATIONAL-3.
NK0813           20 OMGPR-D-EFF-JIT-NON-OM-SLCT       PIC X(8)
NK0813                                     VALUE     SPACE.
NK0813           20 OMGPR-D-START-JIT-NON-OM-SLCT     PIC X(8)
NK0813                                     VALUE     SPACE.
NK0415           20 OMGPR-F-BREAK-BULK-OR-LUOM     PIC X(01).
NK0415              88  OMGPR-F-BREAK-BULK-FEE     VALUE 'B'.
NK0415              88  OMGPR-F-LUOM-FEE           VALUE 'L'.
NK0415           20 OMGPR-P-ACTUAL-BB-OR-LUOM-PCT
NK0415                                             PIC S9(1)V9(4)
NK0415                                              USAGE COMP-3.
NK0615        10 OMGPR-PARM-LENGTH         PIC S9(4)   VALUE +1789.
NK0615        10 OMGPR-NDP-CALLED-YN       PIC X(1)    VALUE SPACE.
NK0615           88 OMGPR-NDP-WAS-CALLED   VALUE     'Y'.
NK0616        10 OMGPR-F-NEGATIVE-REBATE   PIC X(1)    VALUE SPACE.    
NK0616        10 OMGPR-F-CNT-PRIORITY      PIC X(1)    VALUE SPACE. 
NK0618        10 OMGPR-I-PARENT-VENDOR     PIC X(4)    VALUE SPACE.
BM0821        10 OMGPR-F-NON-SANC-RATING   PIC X(1)    VALUE SPACE.
BM0821        10 OMGPR-C-RATING-FLAG       PIC X(2)    VALUE SPACE.     
SJ0821        10 OMGPR-HC-GROUP-ID         PIC 9(08)   VALUE ZERO.
SJ0821        10 OMGPR-F-HC-COST-FLAG      PIC X(01)   VALUE SPACE.
SJ0821        10 OMGPR-F-HC-SELL-FLAG      PIC X(01)   VALUE SPACE.
SJ0821        10 OMGPR-D-HC-COST-EFF       PIC X(10)   VALUE SPACE.
SJ0821        10 OMGPR-D-HC-SELL-EFF       PIC X(10)   VALUE SPACE.
KS0722        10 OMGPR-FULL-LUM-SVC-SW     PIC X(01)   VALUE SPACE. 
BM0722        10 OMGPR-SPCL-SRVC-CODE      PIC X(3)    VALUE SPACE.
     **       * PANDAC
SJ0722*       10 OMGPR-A-PANDAC-FEE        PIC S9(7)V9(8)            
SJ0722*                                                VALUE 0 COMP-3. 
SJ0722        10 OMGPR-A-PANDAC-PN         PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
SJ0722        10 OMGPR-P-PANDAC-PN         PIC S9(3)V9(4)         
SJ0722                                                 VALUE 0 COMP-3.
DP0722        10 OMGPR-FEE-SHRT-CODE-PN    PIC X(02)   VALUE SPACE.
DP0722        10 OMGPR-SKU-CODE-PN         PIC X(04)   VALUE SPACE.
DP0722        10 OMGPR-BILLING-FRQ-PN      PIC X(02)   VALUE SPACE.
SJ0722           88 OMGPR-DAILY-BILL-PN                VALUE 'DL'.
SJ0722           88 OMGPR-DAILY-BILL-SEP-PN            VALUE 'DS'.
SJ0722           88 OMGPR-MONTHLY-AUTO-BILL-PN         VALUE 'MA'.
SJ0722           88 OMGPR-MONTHLY-MANUAL-BILL-PN       VALUE 'MM'.
SJ0722        10 OMGPR-FEE-TYPE-PN      PIC X(01)      VALUE SPACE.
SJ0722           88 OMGPR-FEE-PER-LINE-PN           VALUE 'L'.
SJ0722           88 OMGPR-FEE-PER-QTY-PN            VALUE 'Q'.
SJ0722           88 OMGPR-FEE-PER-ORDER-PN          VALUE 'O'.
SJ0722           88 OMGPR-FEE-PCT-SELL-PN           VALUE 'P'.
SJ0722           88 OMGPR-FEE-PCT-COST-PN           VALUE 'C'.
     **       * SURGITRAK
SJ0722        10 OMGPR-A-SURGITRAK-FEE     PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
     **       * STANDARD DIST MARKUP GROUP SANCTION
SJ0722        10 OMGPR-A-MKP-GRP-SAN-GS    PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
     **       * STANDARD DIST MARKUP GROUP NON-SANCTION
SJ0722        10 OMGPR-A-MKP-GRP-NSC-GN    PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
     **       * STANDARD DIST MARKUP INDIVIDUAL
SJ0722        10 OMGPR-A-MKP-IND-MI        PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
     **       * STANDARD DIST MARKUP NON-CONTRACT
SJ0722        10 OMGPR-A-MKP-NON-CON-MN    PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
     **       * STANDARD DIST MARKUP CUSTOM
SJ0722        10 OMGPR-A-MKP-CUS-MC        PIC S9(7)V9(8)            
SJ0722                                                 VALUE 0 COMP-3. 
NK0618*       10 OMGPR-FILLER              PIC X(470).
BM0421*       10 OMGPR-FILLER              PIC X(464).      
BM0821*       10 OMGPR-FILLER              PIC X(461).      
SJ0821*       10 OMGPR-FILLER              PIC X(431).  
DP0722        10 OMGPR-FILLER              PIC X(217). 