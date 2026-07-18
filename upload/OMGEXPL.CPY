000001****************************************                          00000001
000002* OMGEXPL                                                         00000002
000002* ------                                                          00000003
000002*  THIS IS DATA STRUCTURE USED TO TRANSFER DATA BETWEEN ANY CALLIN00000004
000002*  -G PROCESS & A6U12. THIS CONTAINS RESULTS OF EXPLOSION PROCESS.00000005
000004* CHANGE LOG:                                                     00000006
000013****************************************                          00000037
000014*01  OMGEXPL.                                                     00000038
000015****************************************                          00000039
000016****************************************                          00000040
000017     02 OMGEXPL-C.                                                00000041
000018        10 OMGEXPL-INPUT-FIELDS.                                  00000042
000019           15 OMGEXPL-PACK-PRODNO               PIC X(12).        00000043
000021           15 OMGEXPL-INP-EFF-DATE              PIC X(10).        00000045
000021           15 OMGEXPL-REQUEST-TYPE              PIC X(01).        00000047
      * GET ONLY COMPONENTS ( NO SUB PACKS )
                    88 OMGEXPL-GET-COMP-ONLY   VALUE 'C'.
      * GET COMPONENTS &  SUB PACKS AT LEVEL 1 ONLY ( NO LEVEL 2 )
                    88 OMGEXPL-GET-LEVL-ONLY   VALUE 'L'.
      * GET COMPONENTS &  SUB PACKS PROVIDING STRUCTURAL INFORMATION
                    88 OMGEXPL-GET-STRUCTURE   VALUE 'S'.
      * DEFAULT - GET ONLY COMPONENTS ( NO SUB PACKS )
                    88 OMGEXPL-DEFAULT         VALUE ' '.
                 15 OMGEXPL-ROLLUP-COST-SW            PIC X(01).        00000047
      * ADD UP RESPECTIVE OH & THIRD PARTY FEES OF PACK & SUB PACKS
                    88 OMGEXPL-ROLLUP-COSTS    VALUE 'Y'.
      * DON'T ADD RESPECTIVE OH & THIRD PARTY FEES OF PACK & SUB PACKS
                    88 OMGEXPL-NO-ROLLUP-COSTS    VALUE 'N'.
      * DEFAULT -  ADD RESPECTIVE OH & THIRD PARTY FEES
                    88 OMGEXPL-DEFAULT-ROLLUP  VALUE ' '.
000166           15 OMGEXPL-T-NEXT-PRODUCT-NO         PIC X(08).        00000049
000166           15 OMGEXPL-T-INPUT-FILLER            PIC X(24).        00000049
000168        10 OMGEXPL-PRC-OUTPUT-FIELDS.                             00000215
000169           15 OMGEXPL-PRODUCT-TYPE             PIC X(01).         00000216
000169           15 OMGEXPL-PACK-STATUS              PIC X(01).         00000217
000169           15 OMGEXPL-CUSTOM-SOURCE            PIC X(01).         00000218
000169           15 OMGEXPL-BASE-UOM                 PIC X(02).         00000219
000169           15 OMGEXPL-OH-FEE                   PIC 9(05)V9(08).   00000220
000169           15 OMGEXPL-OH-EFF-DATE              PIC X(10).         00000221
000169           15 OMGEXPL-OH-EXP-DATE              PIC X(10).         00000222
000169           15 OMGEXPL-TP-FEE-COST              PIC 9(05)V9(08).   00000223
000169           15 OMGEXPL-TP-COST-EFF-DATE         PIC X(10).         00000224
000169           15 OMGEXPL-TP-COST-EXP-DATE         PIC X(10).         00000225
000169           15 OMGEXPL-TP-FEE-SELL              PIC 9(05)V9(08).   00000226
000169           15 OMGEXPL-TP-SELL-EFF-DATE         PIC X(10).         00000227
000169           15 OMGEXPL-TP-SELL-EXP-DATE         PIC X(10).         00000228
000169           15 OMGEXPL-NUMBER-OF-ITEMS          PIC 9(03).         00000229
000169           15 OMGEXPL-COMPLETE-EXPLODE-SW      PIC X(01).         00000230
000169           15 OMGEXPL-NEXT-SUBPACK-PROD-NO     PIC X(12).         00000231
000169           15 OMGEXPL-COMP-INFO OCCURS 700 TIMES.                 00000232
                     20 OMGEXPL-SUB-PACK-NUM         PIC X(12).         00000233
                     20 OMGEXPL-COMP-PROD-NO         PIC X(12).         00000234
                     20 OMGEXPL-COMP-UOM             PIC X(02).         00000235
                     20 OMGEXPL-COMP-QTY             PIC 9(05).         00000236
                     20 OMGEXPL-COMP-PROD-TYPE       PIC X(01).         00000237
                     20 OMGEXPL-LEVEL                PIC 9(01).         00000238
                 15 OMGEXPL-ERROR-RESPONSE-FLAG      PIC X(01).         00000239
                     88 OMGEXPL-ABEND    VALUE 'A'.                     00000240
                     88 OMGEXPL-ERROR    VALUE 'E'.                     00000241
                     88 OMGEXPL-WARNING  VALUE 'W'.                     00000242
                 15 OMGEXPL-ERROR-NUMBER             PIC X(05).         00000243
                 15 OMGEXPL-ERROR-MSG                PIC X(80).         00000244
                 15 OMGEXPL-SQL-CODE                 PIC X(05).         00000245
