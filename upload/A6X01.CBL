000010 IDENTIFICATION DIVISION.                                         00000010
000020 PROGRAM-ID. A6X01 INITIAL.                                       00000011
000030 AUTHOR. SATHYA.                                                  00000012
000040 DATE-WRITTEN.  01-30-03.                                         00000013
000050*REMARKS.                                                         00000050
000060*    ***********************************************************  00000060
000070*    * COMPILE = CICS/DB2                                      *  00000061
000080*    *                                                         *  00000080
000090*    * PROGRAM NAME: A6X01                                     *  00000081
000100*    *                                                         *  00000100
000110*    * PURPOSE:                                                *  00000110
000120*    *     THIS PROGRAM IS LINKED TO EXECUTE PRICER SUBROUTINE *  00000111
000130*    *     A6U01. THIS PROGRAM IN EFFECT HAS BECOME PROXY      *  00000112
000130*    *     FOR ACTUAL PRICER WHICH IS A6U01                    *  00000113
000140*    *                                                         *  00000140
000150*    * REPORT DESCRIPTION:                                     *  00000150
000160*    *                                                         *  00000160
000170*    * REVISION INFO:                                          *  00000170
000180*    * CONTROL #     PROGRAMMER          STARTED    COMPLETED  *  00000180
MFMIGR*----------------------------------------------------------------*00000181
MFMIGR* 04/08/06  CR050057   JIM NOLAN   MODIFIED SET STATEMENT TO     *00000182
MFMIGR*           MFMIGR                 RETRIEVE CURRENT DATE IN ORDER*00000183
MFMIGR*                                  TO WORK WITH ANSI COMPLIANT   *00000184
MFMIGR*                                  DBRM DATABASES.               *00000185
MFMIGR*----------------------------------------------------------------*00000186
000190*    * SA0703 KITING SATHYA                         07/31/03      00000188
000190*    *                  ADDED LOGIC TO LINK TO A6O010U IF         00000189
000190*    *                  PROD TYPE NOT GIVEN IN OMGPR              00000190
000190*    * NEW           SATHYA              01/30/03   04/18/03      00000200
000210*    ***********************************************************  00000210
000220*                                                                 00000220
000230 DATA DIVISION.                                                   00000230
000240 WORKING-STORAGE SECTION.                                         00000240
000250*                                                                 00000250
000260 77  WS-REVISION-LEVEL-STAMP     PIC X(8)   VALUE 'BW 99999'.     00000260
000260 77  WS-CURRENT-DATE             PIC X(10).                       00000261
       01 WS-MISC.                                                      00000262
          10 WS-XCTL-CICS-RESP-X.                                       00000263
           15 WS-XCTL-CICS-RESP     PIC S9(8)                           00000264
                                    VALUE     ZERO.                     00000265
SA0703    10 WS-ERR-MSG.                                                00000267
SA0703      15 WS-ERR-NUM             PIC X(05).                        00000269
SA0703      15 WS-ERROR-MESSAGE       PIC X(70).                        00000270
SA0703    10 WS-SQL-ERR-MSG.                                            00000271
SA0703      15 WS-SQL-ERR-NUM         PIC X(05).                        00000272
SA0703      15 WS-SQL-ERROR-MESSAGE   PIC X(70).                        00000273
SA0703 01  WS-CONSTANTS.                                                00000274
             05 WS-PRICING-REQUEST    PIC X(01) VALUE 'P'.              00000275
             05 WS-REGULAR-COMPONENT  PIC X(01) VALUE 'R'.              00000278
       01 A6R10-REC.                                                    00000279
SA0703      COPY A6R10.                                                 00000280
            EXEC SQL                                                    00000281
              INCLUDE DFHRESP                                           00000282
            END-EXEC.                                                   00000283
            EXEC SQL                                                    00000284
               INCLUDE SQLCA                                            00000285
            END-EXEC.                                                   00000286
000291                          COPY OMGPR.                             00000287
000270*                                                                 00000288
000280 LINKAGE SECTION.                                                 00000289
       01  DFHCOMMAREA.                                                 00000290
           05  COMMAREA PIC X                                           00000291
               OCCURS 1 TO 32767                                        00000292
               DEPENDING ON EIBCALEN.                                   00000300
000310                                                                  00000310
000320 PROCEDURE DIVISION.                                              00000311
000340                                                                  00000340
000350 000-LINK-SUBROUTINE.                                             00000350
           IF EIBCALEN > 0                                              00000364
             MOVE DFHCOMMAREA TO OMGPR                                  00000365
SA0703       PERFORM 025-GET-DATE                                       00000366
SA0703          THRU 025-EXIT                                           00000367
SA0703       PERFORM 200-LINK-A6O010                                    00000368
SA0703         THRU  200-EXIT                                           00000369
SA0703       IF A6W10-ABEND  OR                                         00000370
SA0703          A6W10-ERROR                                             00000371
SA0703          MOVE 'Y' TO OMGPR-F-PRICER-ERROR                        00000372
SA0703          PERFORM 9010-MOVE-ERROR                                 00000373
SA0703             THRU 9010-EXIT                                       00000374
SA0703          MOVE WS-ERR-MSG     TO OMGPR-ERROR-MESSAGE              00000375
SA0703          MOVE OMGPR TO DFHCOMMAREA                               00000376
SA0703       ELSE                                                       00000377
SA0703         PERFORM 050-PROCESS-PRICE                                00000378
SA0703            THRU 050-EXIT                                         00000379
               MOVE OMGPR TO DFHCOMMAREA                                00000380
             END-IF                                                     00000381
           ELSE                                                         00000382
              MOVE 'Y' TO OMGPR-F-PRICER-ERROR                          00000383
              MOVE ' NO DATA RECEIVED BY A6X01' TO                      00000384
                          OMGPR-ERROR-MESSAGE                           00000385
              MOVE OMGPR TO DFHCOMMAREA                                 00000386
           END-IF.                                                      00000390
000420     GOBACK.                                                      00000420
SA0703 025-GET-DATE.                                                    00000421
           EXEC SQL                                                     00000422
MFMIGR        SELECT CONVERT(CHAR(10),CURRENT_TIMESTAMP,101)            00000425
MFMIGR               INTO :WS-CURRENT-DATE                              00000425
MFMIGR                                                                  00000426
           END-EXEC.                                                    00000427
           EVALUATE SQLCODE                                             00000428
              WHEN 0                                                    00000429
                 CONTINUE                                               00000430
              WHEN OTHER                                                00000431
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000432
                 MOVE 'A6X01- SQL ERROR  '                              00000433
                                         TO WS-SQL-ERROR-MESSAGE        00000434
                 MOVE SQLCODE             TO WS-SQL-ERR-NUM             00000435
                 MOVE WS-SQL-ERR-MSG      TO OMGPR-ERROR-MESSAGE        00000436
           END-EVALUATE.                                                00000437

SA0703 025-EXIT.                                                        00000438
SA0703     EXIT.                                                        00000439
SA0703 050-PROCESS-PRICE.                                               00000440
SA0703     IF OMGPR-PRICER-ERROR                                        00000441
SA0703       GO TO 050-EXIT                                             00000442
SA0703     END-IF.                                                      00000443
SA0703     IF A6W10-PROD-TYPE = 'O'                                     00000444
SA0703        PERFORM 110-LINK-TO-A6U11                                 00000445
SA0703          THRU  110-EXIT                                          00000446
SA0703     ELSE                                                         00000447
SA0703         PERFORM 100-LINK-TO-PRICER                               00000448
SA0703            THRU 100-EXIT                                         00000449
SA0703     END-IF.                                                      00000450
SA0703 050-EXIT.                                                        00000451
SA0703     EXIT.                                                        00000452
       100-LINK-TO-PRICER.                                              00000453
           INITIALIZE  OMGPR-DATA-FOR-PACKS.                            00000454
           INITIALIZE  OMGPR-PRC-OUTPUT-FIELDS.                         00000455
           MOVE WS-PRICING-REQUEST                                      00000456
                             TO OMGPR-PRICING-REQ-SW.                   00000457
           MOVE WS-REGULAR-COMPONENT                                    00000458
                             TO OMGPR-INPUT-PRODUCT-TYPE.               00000459
           EXEC CICS LINK                                               00000460
                     PROGRAM  ('A6U01')                                 00000461
                     COMMAREA (OMGPR)                                   00000462
                     RESP (WS-XCTL-CICS-RESP)                           00000463
           END-EXEC.                                                    00000464
           EVALUATE TRUE                                                00000465
               WHEN WS-XCTL-CICS-RESP =                                 00000466
                    DFHRESP-NORMAL                                      00000467
                      MOVE WS-REGULAR-COMPONENT                         00000468
                             TO OMGPR-C-PRODUCT-TYPE                    00000469
               WHEN WS-XCTL-CICS-RESP =                                 00000470
                    DFHRESP-PGMIDERR                                    00000471
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000472
                 MOVE                                                   00000473
                    'A6U01 TRANSACTION IS UNAVAILABLE'                  00000474
                                         TO OMGPR-ERROR-MESSAGE         00000475
               WHEN OTHER                                               00000476
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000477
                 MOVE 'ERROR-UNABLE TO LINK TO A6U01'                   00000478
                                         TO OMGPR-ERROR-MESSAGE         00000479
           END-EVALUATE.                                                00000480
       100-EXIT.                                                        00000481
           EXIT.                                                        00000482
SA0703 110-LINK-TO-A6U11.                                               00000483
SA0703     INITIALIZE OMGPR-PRC-OUTPUT-FIELDS.                          00000484
           EXEC CICS LINK                                               00000485
                     PROGRAM  ('A6O011U')                               00000486
                     COMMAREA (OMGPR)                                   00000487
                     RESP (WS-XCTL-CICS-RESP)                           00000488
           END-EXEC.                                                    00000489
           EVALUATE TRUE                                                00000490
               WHEN WS-XCTL-CICS-RESP =                                 00000491
                    DFHRESP-NORMAL                                      00000492
                   CONTINUE                                             00000493
               WHEN WS-XCTL-CICS-RESP =                                 00000494
                    DFHRESP-PGMIDERR                                    00000495
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000496
                 MOVE                                                   00000497
                    'A6O011 TRANSACTON IS UNAVAILABLE'                  00000498
                                         TO OMGPR-ERROR-MESSAGE         00000499
               WHEN OTHER                                               00000500
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000501
                 MOVE 'ERROR-UNABLE TO LINK TO A6O011U'                 00000502
                                         TO OMGPR-ERROR-MESSAGE         00000503
           END-EVALUATE.                                                00000504
       110-EXIT.                                                        00000505
           EXIT.                                                        00000506
SA0703 200-LINK-A6O010.                                                 00000507
SA0703     IF OMGPR-PRICER-ERROR                                        00000508
SA0703       GO TO 200-EXIT                                             00000509
SA0703     END-IF.                                                      00000510
           INITIALIZE A6W10-RTND-DATA.                                  00000511
SA0703     MOVE OMGPR-I-VENDOR           TO A6W10-I-VENDOR              00000512
SA0703     MOVE OMGPR-I-VND-PRODUCT      TO A6W10-I-VND-PRODUCT         00000513
SA0703     EXEC CICS LINK                                               00000514
SA0703               PROGRAM  ('A6O010U')                               00000515
SA0703               COMMAREA (A6R10-REC)                               00000516
SA0703               RESP (WS-XCTL-CICS-RESP)                           00000517
SA0703     END-EXEC.                                                    00000518
           EVALUATE TRUE                                                00000519
               WHEN WS-XCTL-CICS-RESP =                                 00000520
                    DFHRESP-NORMAL                                      00000521
                   IF ( A6W10-ABEND OR                                  00000522
                        A6W10-ERROR )                                   00000523
                     MOVE A6W10-ERROR-MESSAGE                           00000524
                                         TO OMGPR-ERROR-MESSAGE         00000525
                     MOVE 'Y'            TO OMGPR-F-PRICER-ERROR        00000526
                   ELSE                                                 00000527
                     MOVE A6W10-PROD-TYPE                               00000528
                                         TO OMGPR-INPUT-PRODUCT-TYPE    00000529
                                            OMGPR-C-PRODUCT-TYPE        00000530
                   END-IF                                               00000531
               WHEN WS-XCTL-CICS-RESP =                                 00000532
                    DFHRESP-PGMIDERR                                    00000533
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000534
                 MOVE                                                   00000535
                    'A6O010U TRANSACTION IS UNAVAILABLE'                00000536
                                         TO OMGPR-ERROR-MESSAGE         00000537
               WHEN OTHER                                               00000538
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR        00000539
                 MOVE 'ERROR-UNABLE TO LINK TO A6O010U'                 00000540
                                         TO OMGPR-ERROR-MESSAGE         00000541
           END-EVALUATE.                                                00000542
SA0703 200-EXIT.                                                        00000543
           EXIT.                                                        00000544
SA0703*                                                                 00000545
SA0703 9010-MOVE-ERROR.                                                 00000546
SA0703     IF A6W10-ERROR-SQLCODE > SPACES                              00000547
SA0703          MOVE A6W10-ERROR-SQLCODE TO WS-SQL-ERR-NUM              00000548
SA0703          MOVE A6W10-ERROR-MESSAGE TO WS-SQL-ERROR-MESSAGE        00000549
SA0703          MOVE WS-SQL-ERR-MSG      TO WS-ERR-MSG                  00000550
SA0703      ELSE                                                        00000551
SA0703          MOVE A6W10-ERROR-SQLCODE TO WS-ERR-NUM                  00000552
SA0703          MOVE A6W10-ERROR-MESSAGE TO WS-ERROR-MESSAGE            00000553
SA0703      END-IF.                                                     00000554
SA0703 9010-EXIT.                                                       00000560
SA0703     EXIT.                                                        00000600
SA0703*                                                                 00000700
