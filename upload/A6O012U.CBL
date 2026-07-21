       IDENTIFICATION DIVISION.
       PROGRAM-ID.                     A6O012U IS INITIAL.
       AUTHOR.                         SATHYA.
      ************************  REMARKS  ***************************
      *    * COMPILE = PN RQ                                      *
      *    * TYPE    = 3                                          *
      **                                                          **
      **  PROJECT NUMBER: KITTING                                 **
      **                                                          **
      **  IMPLEMENTATION DATE:  07/31/2003                        **
      **                                                          **
      **  PROGRAM DEVELOPER: SATHYA                               **
      **                                                          **
      **  DESCRIPTION:  ONLINE COMPONENT TO PROCESS THE REQUEST   **
      **                FOR EXPLOSION                             **
      **                1. THIS INVOKES ANOTHER COMPONENT A6O015U **
      **                   TO CARRY OUT EXPLOSION                 **
      **                2. GOES THROUGH RESULT GOT IN OMGPK &     **
      **                   FILLSUP OMGEXPL AS PER REQUEST TYPE    **
      **                   WHICH CAN BE GET COMPONENTS WITH STRUCT**
      **                   -URE INFO OR GET COMP & SUBPACK AT LEVL**
      **                   - 1 ONLY OR GET COMPONENTS AT LEVEL 1  **
      **                   & 2                                    **
      **  NOTES:                                                  **
      **  MODIFICATION LOG:                                       **
      **     DATE  PRGM DESCRIPTION                               **
      **--------------------------------------------------------- **
MFMIGR* 04/08/06  CR050057   JIM NOLAN   MODIFIED SET STATEMENT TO     *
MFMIGR*           MFMIGR                 RETRIEVE CURRENT DATE IN ORDER*
MFMIGR*                                  TO WORK WITH ANSI COMPLIANT   *
MFMIGR*                                  DBRM DATABASES.               *
MFMIGR*----------------------------------------------------------------*
NK1209**                RELEASE 4.2                               **
NK1209**  12/09/04 INK  INCIDENT # 1667882 - PACK WITH 4 SUBPACKS **
NK1209**                W/ THE SAME PRODUCT # ONLY BEING COSTING  **
NK1209**                1 TIME INSTEAD OF 4.                      **
      **************************************************************
       ENVIRONMENT DIVISION.
       DATA DIVISION.
      *****************************************************************
      ***   W O R K I N G     S T O R A G E      S E C T I O N       **
      *****************************************************************
       WORKING-STORAGE SECTION.
       01  WS.
           05  WS-MARKER                    PIC X(14)
                                            VALUE 'A6O012 BGINS>>'.
           05  WS-TRANSID                   PIC X(4)
                                            VALUE 'A612'.
           05  WS-STATUS-CODES.
               10 WS-XCTL-CICS-RESP-X.
                 15  WS-XCTL-CICS-RESP      PIC S9(08).
           05  SUB                          PIC 999 VALUE 0.
           05  SUB-SEARCH                   PIC 999 VALUE 0.
           05  SUB-OMEXPL                   PIC 999 VALUE 0.
           05  SUB-SUBPACK                  PIC 999 VALUE 0.
           05  WS-CONSTANTS.
               10  FILLER                   PIC X(12)
                                            VALUE 'CONSTANTS==>'.
               10  WS-CURRENT-PROGRAM       PIC X(08)
                                            VALUE 'A6O012U '.
               10  WS-CURRENT-TRANSACTION   PIC X(04)
                                            VALUE 'A612'.
               10  WS-MAX-ITEMS-IN-PACK     PIC 999 VALUE 700.
               10  WS-MAX-SUB-PACKS         PIC 999 VALUE 300.
               10  WS-REQUEST-TYPES.
                   15 WS-REQUEST-STRUCTURE  PIC X(1) VALUE 'S'.
                   15 WS-REQUEST-FIRST-LEVEL PIC X(1) VALUE 'L'.
                   15 WS-REQUEST-COMPONENT  PIC X(1) VALUE 'C'.
                   15 WS-REQUEST-DEFAULT    PIC X(1) VALUE 'C'.
               10  WS-ERROR-TYPES.
                   15 WS-ABEND-ERROR        PIC X(01) VALUE 'A'.
                   15 WS-NONABEND-ERROR     PIC X(01) VALUE 'E'.
                   15 WS-WARNING            PIC X(01) VALUE 'W'.
               10  WS-NEXT-ERROR-NBR-X.
                   15  WS-NEXT-ERROR-NBR    PIC 9(03)
                                            VALUE 003.
               10 WS-LENGTH-OF-COM-AREA     PIC 9(05) VALUE 24000.
           05  WS-WORK-AREAS.
               10  FILLER                   PIC X(12)
                                            VALUE 'WORK AREAS=>'.
               10 WS-LENGTH-TO-PASS         PIC S9(4) USAGE COMP SYNC
                                             VALUE ZERO.
               10 WS-EDITED-SQLCODE         PIC -Z999.
               10 A6W015U-PROGRAM       PIC X(08)      VALUE 'A6O015U'.
               10 WS-PARENT-OF-COMP         PIC X(12).
NK1209         10 WS-PARENT-OF-COMP-QTY     PIC 9(05).
               10 WS-DATE-REC.
                 15 WS-DATE-X               PIC X(10).
                 15 WS-DATE-REC-NUM REDEFINES WS-DATE-X.
                    20 WS-DATE-MM           PIC 99.
                    20 WS-DATE-SLASH-1      PIC X.
                    20 WS-DATE-DD           PIC 99.
                    20 WS-DATE-SLASH-2      PIC X.
                    20 WS-DATE-YYYY         PIC 9(04).
               10 WS-SUB-PACK-ARRAY.
                  15 WS-SUB-PACK OCCURS 300 TIMES.
                     20 WS-SUB-PACK-PROD-NO PIC X(12).
                     20 WS-SUB-PACK-SL-NO   PIC 9(03).
NK1209               20 WS-SUB-PACK-QTY     PIC 9(05).
               10  WS-CURRENT-DATE          PIC X(10).
           05  WS-SWITCHES.
               10  WS-SUB-PACK-FOUND-SW     PIC X VALUE 'N'.
                   88 WS-SUB-PACK-FOUND    VALUE 'Y'.
               10  WS-NOMORE-TO-PROCESS-SW  PIC X VALUE 'N'.
                   88 WS-NOMORE-TO-PROCESS VALUE 'Y'.
               10  WS-NO-MORE-SEARCH-SW     PIC X VALUE 'N'.
                   88 WS-NO-MORE-SEARCH      VALUE 'Y'.
               10  WS-ERROR-SW              PIC X(01).
                   88 WS-ABEND               VALUE 'A'.
                   88 WS-ERROR               VALUE 'E'.
                   88 WS-WARNING             VALUE 'W'.
          COPY SYR000.
          COPY SYR208.
       01 WS-WHAT-TO-PASS.
                       COPY OMGEXPL.
       01 A6W015U-PARM-LIST.
                       COPY OMGPK.
           EXEC SQL
              INCLUDE DFHRESP
           END-EXEC.
           EXEC SQL
              INCLUDE SQLCA
           END-EXEC.
      *****************************************************************
      ***              L I N K A G E          S E C T I O N          **
      *****************************************************************
       LINKAGE SECTION.
       01  DFHCOMMAREA.
           05  COMMAREA   PIC X(23367).
      *
       PROCEDURE DIVISION.
           PERFORM 0010-INIT
             THRU  0010-EXIT.
           PERFORM 0015-VALIDATE
             THRU  0015-EXIT.
           IF  OMGEXPL-ABEND OR
               OMGEXPL-ERROR
                   CONTINUE
           ELSE
              PERFORM 0020-PROCESS
                THRU  0020-EXIT
           END-IF.
           PERFORM 0030-EOJ
             THRU  0030-EXIT.
      *
       0010-INIT.
           MOVE WS-LENGTH-OF-COM-AREA    TO WS-LENGTH-TO-PASS.
           MOVE DFHCOMMAREA              TO WS-WHAT-TO-PASS.
           INITIALIZE OMGEXPL-PRC-OUTPUT-FIELDS
                      OMGPK-C
                      WS-SUB-PACK-ARRAY.
            EXEC SQL
MFMIGR        SELECT CONVERT(CHAR(10),CURRENT_TIMESTAMP,101)
MFMIGR               INTO :WS-CURRENT-DATE
MFMIGR
            END-EXEC.
            EVALUATE SQLCODE
               WHEN 0
                  CONTINUE
               WHEN OTHER
                  MOVE WS-ABEND-ERROR     TO OMGEXPL-ERROR-RESPONSE-FLAG
                  MOVE '61210- SQL ERROR  '
                                          TO OMGEXPL-ERROR-MSG
                  MOVE SQLCODE            TO WS-EDITED-SQLCODE
                  MOVE WS-EDITED-SQLCODE  TO OMGEXPL-SQL-CODE
                  MOVE  61210             TO OMGEXPL-ERROR-NUMBER
            END-EVALUATE.

       0010-EXIT.
           EXIT.
      *
       0015-VALIDATE.
            IF OMGEXPL-ABEND OR
               OMGEXPL-ERROR
               GO TO 0015-EXIT
            END-IF.
            IF OMGEXPL-PACK-PRODNO = SPACES
               MOVE WS-NONABEND-ERROR    TO OMGEXPL-ERROR-RESPONSE-FLAG
               MOVE '61201- PRODUCT# NOT PROVIDED'
                                         TO OMGEXPL-ERROR-MSG
               MOVE  61201               TO OMGEXPL-ERROR-NUMBER
            END-IF.
            IF OMGEXPL-ABEND OR
               OMGEXPL-ERROR
               GO TO 0015-EXIT
            END-IF.
            IF OMGEXPL-INP-EFF-DATE > SPACES
               PERFORM 0018-VALIDATE-DATE
                 THRU  0018-EXIT
             IF SYU208-OUT-STATUS-GOOD
                 CONTINUE
             ELSE
               MOVE WS-NONABEND-ERROR    TO OMGEXPL-ERROR-RESPONSE-FLAG
               MOVE '61202- EFF DATE IS INVALID  '
                                         TO OMGEXPL-ERROR-MSG
               MOVE  61202               TO OMGEXPL-ERROR-NUMBER
             END-IF
            ELSE
              MOVE WS-CURRENT-DATE       TO OMGEXPL-INP-EFF-DATE
            END-IF.
            IF OMGEXPL-ABEND OR
               OMGEXPL-ERROR
               GO TO 0015-EXIT
            END-IF.
            EVALUATE OMGEXPL-ROLLUP-COST-SW
              WHEN LOW-VALUES
                CONTINUE
              WHEN 'Y'
                CONTINUE
              WHEN 'N'
                CONTINUE
              WHEN ' '
                CONTINUE
              WHEN  OTHER
                   MOVE WS-NONABEND-ERROR
                                         TO OMGEXPL-ERROR-RESPONSE-FLAG
                   MOVE '61204- ROLLUP FEES SW INVALID'
                                         TO OMGEXPL-ERROR-MSG
                   MOVE  61204           TO OMGEXPL-ERROR-NUMBER
            END-EVALUATE
            EVALUATE OMGEXPL-REQUEST-TYPE
              WHEN  WS-REQUEST-STRUCTURE
                 CONTINUE
              WHEN  WS-REQUEST-FIRST-LEVEL
                 CONTINUE
              WHEN  WS-REQUEST-COMPONENT
                 CONTINUE
              WHEN  OTHER
                IF OMGEXPL-REQUEST-TYPE = SPACES
                   MOVE WS-REQUEST-DEFAULT
                                         TO OMGEXPL-REQUEST-TYPE
                ELSE
                   MOVE WS-NONABEND-ERROR
                                         TO OMGEXPL-ERROR-RESPONSE-FLAG
                   MOVE '61203- REQ TYPE IS INVALID  '
                                         TO OMGEXPL-ERROR-MSG
                   MOVE  61203           TO OMGEXPL-ERROR-NUMBER
                END-IF
            END-EVALUATE.
            IF OMGEXPL-ABEND OR
               OMGEXPL-ERROR
               GO TO 0015-EXIT
            END-IF.
       0015-EXIT.
           EXIT.
      *
       0018-VALIDATE-DATE.
            MOVE OMGEXPL-INP-EFF-DATE    TO SYU208-IN-DT.
             COPY SYH208.
       0018-EXIT.
           EXIT.
      *
       0020-PROCESS.
           PERFORM 1000-MOVE-INP-OMGPK
             THRU  1000-EXIT.
           IF WS-ABEND
              GO TO 0020-EXIT
           END-IF.
           MOVE 0                        TO WS-XCTL-CICS-RESP.
           PERFORM 2000-LINK-TO-EXPLODER
             THRU  2000-EXIT.
           EVALUATE TRUE
               WHEN WS-XCTL-CICS-RESP =  DFHRESP-NORMAL
                   PERFORM 3000-MOVE-TO-OMGEXPL
                     THRU  3000-EXIT
               WHEN WS-XCTL-CICS-RESP = DFHRESP-PGMIDERR
                 MOVE 'A6U15 TRANSACTION IS UNAVAILABLE'
                                         TO OMGEXPL-ERROR-MSG
                 MOVE WS-NONABEND-ERROR  TO OMGEXPL-ERROR-RESPONSE-FLAG
               WHEN OTHER
                 MOVE 'A6U15 LINK ERROR '
                                         TO OMGEXPL-ERROR-MSG
                 MOVE WS-NONABEND-ERROR  TO OMGEXPL-ERROR-RESPONSE-FLAG
           END-EVALUATE.
       0020-EXIT.
           EXIT.
      *
       0030-EOJ.
           MOVE OMGEXPL-C                TO DFHCOMMAREA.
           PERFORM 8000-PROCESS-RETURN
             THRU  8000-EXIT.
       0030-EXIT.
           EXIT.
      *
       1000-MOVE-INP-OMGPK.
           MOVE OMGEXPL-PACK-PRODNO      TO  OMGPK-PACK-FULL-PRODNO.
           MOVE OMGEXPL-INP-EFF-DATE     TO OMGPK-INP-EFF-DATE.
           MOVE OMGEXPL-ROLLUP-COST-SW   TO OMGPK-ROLLUP-COST-SW.
       1000-EXIT.
           EXIT.
      *
       2000-LINK-TO-EXPLODER.
           EXEC CICS LINK
           PROGRAM  (A6W015U-PROGRAM)
           COMMAREA (A6W015U-PARM-LIST)
           RESP (WS-XCTL-CICS-RESP)
           END-EXEC.
       2000-EXIT.
           EXIT.
      *
       3000-MOVE-TO-OMGEXPL.
           MOVE 'N'                      TO WS-NOMORE-TO-PROCESS-SW.
           MOVE 0                        TO SUB-SUBPACK.
           PERFORM 3100-MOVE-HDR-DATA
             THRU  3100-EXIT.
           MOVE 0                        TO  SUB-OMEXPL.
           PERFORM VARYING SUB FROM 1 BY 1
               UNTIL SUB >= WS-MAX-ITEMS-IN-PACK
               OR SUB >  OMGPK-NUMBER-OF-ITEMS
               OR WS-ABEND
               OR WS-NOMORE-TO-PROCESS
               PERFORM 3050-HANDLE-ASPER-REQUEST
                 THRU  3050-EXIT
           END-PERFORM.
           MOVE SUB-OMEXPL               TO OMGEXPL-NUMBER-OF-ITEMS.
       3000-EXIT.
           EXIT.
      *
       3050-HANDLE-ASPER-REQUEST.
               EVALUATE TRUE
                  WHEN WS-REQUEST-STRUCTURE = OMGEXPL-REQUEST-TYPE
                     PERFORM 3200-PROCESS-STRUCTURE
                       THRU 3200-EXIT
                  WHEN WS-REQUEST-FIRST-LEVEL = OMGEXPL-REQUEST-TYPE
                     PERFORM 3300-PROCESS-LEVEL
                       THRU 3300-EXIT
                  WHEN WS-REQUEST-COMPONENT = OMGEXPL-REQUEST-TYPE
                     PERFORM 3400-PROCESS-COMPONENT
                       THRU 3400-EXIT
                  WHEN OTHER
                      CONTINUE
               END-EVALUATE.
       3050-EXIT.
           EXIT.
      *
       3100-MOVE-HDR-DATA.
            MOVE OMGPK-PRODUCT-TYPE      TO OMGEXPL-PRODUCT-TYPE.
            MOVE OMGPK-CUSTOM-SOURCE     TO OMGEXPL-CUSTOM-SOURCE.
            MOVE OMGPK-BASE-UOM          TO OMGEXPL-BASE-UOM.
            MOVE OMGPK-OH-FEE            TO OMGEXPL-OH-FEE.
            MOVE OMGPK-OH-EFF-DATE       TO OMGEXPL-OH-EFF-DATE.
            MOVE OMGPK-OH-EXP-DATE       TO OMGEXPL-OH-EXP-DATE.
            MOVE OMGPK-TP-FEE-COST       TO OMGEXPL-TP-FEE-COST.
            MOVE OMGPK-TP-COST-EFF-DATE  TO OMGEXPL-TP-COST-EFF-DATE
            MOVE OMGPK-TP-COST-EXP-DATE  TO OMGEXPL-TP-COST-EXP-DATE
            MOVE OMGPK-TP-FEE-SELL       TO OMGEXPL-TP-FEE-SELL.
            MOVE OMGPK-TP-SELL-EFF-DATE  TO OMGEXPL-TP-SELL-EFF-DATE
            MOVE OMGPK-TP-SELL-EXP-DATE  TO OMGEXPL-TP-SELL-EXP-DATE
            MOVE OMGPK-NUMBER-OF-ITEMS   TO OMGEXPL-NUMBER-OF-ITEMS.
            MOVE OMGPK-COMPLETE-EXPLODE-SW
                                         TO OMGEXPL-COMPLETE-EXPLODE-SW.
       3100-EXIT.
           EXIT.
      *
       3200-PROCESS-STRUCTURE.
           EVALUATE OMGPK-COMP-PROD-TYPE(SUB)
              WHEN 'S'
      * SUB PACK; IN CASE OF STRUCTURE, WE DO NOT NEED TO REPORT

                ADD 1 TO SUB-SUBPACK
                MOVE OMGPK-COMP-PROD-NO(SUB)
                                         TO
                                        WS-SUB-PACK-PROD-NO(SUB-SUBPACK)
                MOVE OMGPK-SUB-PACK-NUM(SUB)
                                       TO WS-SUB-PACK-SL-NO(SUB-SUBPACK)
NK1209          MOVE OMGPK-COMP-QTY(SUB)
NK1209                                 TO WS-SUB-PACK-QTY(SUB-SUBPACK)
              WHEN '1'
                 ADD 1 TO SUB-OMEXPL
                 MOVE OMGEXPL-PACK-PRODNO TO
                            OMGEXPL-SUB-PACK-NUM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-NO(SUB) TO
                            OMGEXPL-COMP-PROD-NO (SUB-OMEXPL)
                 MOVE OMGPK-COMP-UOM (SUB)    TO
                                 OMGEXPL-COMP-UOM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-QTY (SUB)    TO
                                 OMGEXPL-COMP-QTY (SUB-OMEXPL)
                 MOVE 'C'  TO OMGEXPL-COMP-PROD-TYPE(SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-TYPE(SUB)
                                           TO OMGEXPL-LEVEL(SUB-OMEXPL)
      * COMPONENT AT LEVEL 1
              WHEN '2'
      * COMPONENT AT LEVEL 2
      * FIND OUT PROD # OF SUB PACK IN WHICH COMPONENT GOES
                 PERFORM 4000-FIND-PARENT-PROD
                   THRU  4000-EXIT
                 IF WS-SUB-PACK-FOUND
                   ADD 1 TO SUB-OMEXPL
                   MOVE WS-PARENT-OF-COMP TO
                          OMGEXPL-SUB-PACK-NUM (SUB-OMEXPL)
                   MOVE OMGPK-COMP-PROD-NO(SUB) TO
                            OMGEXPL-COMP-PROD-NO (SUB-OMEXPL)
                   MOVE OMGPK-COMP-UOM (SUB)    TO
                                 OMGEXPL-COMP-UOM (SUB-OMEXPL)
NK1209             COMPUTE  OMGEXPL-COMP-QTY (SUB-OMEXPL) =
NK1209              OMGPK-COMP-QTY (SUB) * WS-PARENT-OF-COMP-QTY
                   MOVE 'C'  TO OMGEXPL-COMP-PROD-TYPE(SUB-OMEXPL)
                   MOVE OMGPK-COMP-PROD-TYPE(SUB)
                                           TO OMGEXPL-LEVEL(SUB-OMEXPL)
                 END-IF
      * MOVE TO ARRAY CELL
              WHEN OTHER
      * THIS IS AN ERROR
                CONTINUE
           END-EVALUATE.
       3200-EXIT.
           EXIT.
      *
       3300-PROCESS-LEVEL.
           EVALUATE OMGPK-COMP-PROD-TYPE (SUB)
              WHEN 'S'
      * SUB PACK; IN CASE OF STRUCTURE, WE DO NOT NEED TO REPORT
                 ADD 1 TO SUB-OMEXPL
                 MOVE OMGEXPL-PACK-PRODNO TO
                            OMGEXPL-SUB-PACK-NUM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-NO(SUB) TO
                            OMGEXPL-COMP-PROD-NO (SUB-OMEXPL)
                 MOVE OMGPK-COMP-UOM (SUB)    TO
                                 OMGEXPL-COMP-UOM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-QTY (SUB)    TO
                                 OMGEXPL-COMP-QTY (SUB-OMEXPL)
                 MOVE 'S'  TO OMGEXPL-COMP-PROD-TYPE(SUB-OMEXPL)
              WHEN '1'
                 ADD 1 TO SUB-OMEXPL
                 MOVE OMGEXPL-PACK-PRODNO TO
                            OMGEXPL-SUB-PACK-NUM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-NO(SUB) TO
                            OMGEXPL-COMP-PROD-NO (SUB-OMEXPL)
                 MOVE OMGPK-COMP-UOM (SUB)    TO
                                 OMGEXPL-COMP-UOM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-QTY (SUB)    TO
                                 OMGEXPL-COMP-QTY (SUB-OMEXPL)
                 MOVE 'C'  TO OMGEXPL-COMP-PROD-TYPE(SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-TYPE(SUB)
                                           TO OMGEXPL-LEVEL(SUB-OMEXPL)
      * COMPONENT AT LEVEL 1
              WHEN '2'
      * COMPONENT AT LEVEL 2
      * NOT INTERESTED
                CONTINUE
              WHEN OTHER
      * THIS IS AN ERROR
                CONTINUE
           END-EVALUATE.
       3300-EXIT.
           EXIT.
      *
       3400-PROCESS-COMPONENT.
           EVALUATE OMGPK-COMP-PROD-TYPE(SUB)
              WHEN 'S'
NK1209          ADD 1 TO SUB-SUBPACK
NK1209          MOVE OMGPK-COMP-PROD-NO(SUB)
NK1209                              TO WS-SUB-PACK-PROD-NO(SUB-SUBPACK)
NK1209          MOVE OMGPK-SUB-PACK-NUM(SUB)
NK1209                              TO WS-SUB-PACK-SL-NO(SUB-SUBPACK)
NK1209          MOVE OMGPK-COMP-QTY(SUB)
NK1209                              TO WS-SUB-PACK-QTY(SUB-SUBPACK)
              WHEN '1'
                 ADD 1 TO SUB-OMEXPL
                 MOVE OMGEXPL-PACK-PRODNO TO
                            OMGEXPL-SUB-PACK-NUM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-NO(SUB) TO
                            OMGEXPL-COMP-PROD-NO (SUB-OMEXPL)
                 MOVE OMGPK-COMP-UOM (SUB)    TO
                                 OMGEXPL-COMP-UOM (SUB-OMEXPL)
                 MOVE OMGPK-COMP-QTY (SUB)    TO
                                 OMGEXPL-COMP-QTY (SUB-OMEXPL)
                 MOVE 'C'  TO OMGEXPL-COMP-PROD-TYPE(SUB-OMEXPL)
                 MOVE OMGPK-COMP-PROD-TYPE(SUB)
                           TO OMGEXPL-LEVEL(SUB-OMEXPL)
      * COMPONENT AT LEVEL 1
              WHEN '2'
NK1209           PERFORM 4000-FIND-PARENT-PROD
NK1209             THRU  4000-EXIT
NK1209           IF WS-SUB-PACK-FOUND
                     ADD 1 TO SUB-OMEXPL
      * SINCE COMP ONLY, WE ARE NOT INTERESTED IN KNOWING PARENT#
                     MOVE SPACES                  TO
                                OMGEXPL-SUB-PACK-NUM (SUB-OMEXPL)
                     MOVE OMGPK-COMP-PROD-NO(SUB) TO
                                OMGEXPL-COMP-PROD-NO (SUB-OMEXPL)
                     MOVE OMGPK-COMP-UOM (SUB)    TO
                                OMGEXPL-COMP-UOM (SUB-OMEXPL)
NK1209               COMPUTE  OMGEXPL-COMP-QTY (SUB-OMEXPL) =
NK1209                     OMGPK-COMP-QTY (SUB) * WS-PARENT-OF-COMP-QTY
                     MOVE 'C'  TO OMGEXPL-COMP-PROD-TYPE(SUB-OMEXPL)
                     MOVE OMGPK-COMP-PROD-TYPE(SUB) TO
                               OMGEXPL-LEVEL(SUB-OMEXPL)
NK1209           END-IF
              WHEN OTHER
      * THIS IS AN ERROR
                CONTINUE
           END-EVALUATE.
       3400-EXIT.
           EXIT.
      *
       4000-FIND-PARENT-PROD.
           MOVE 'N' TO WS-SUB-PACK-FOUND-SW
                       WS-NO-MORE-SEARCH-SW.
           PERFORM VARYING SUB-SEARCH FROM 1 BY 1
                   UNTIL SUB-SEARCH > WS-MAX-SUB-PACKS
                   OR WS-SUB-PACK-FOUND
                   OR WS-NO-MORE-SEARCH
              IF  OMGPK-SUB-PACK-NUM (SUB) =
                           WS-SUB-PACK-SL-NO(SUB-SEARCH)
                 MOVE 'Y'                TO WS-SUB-PACK-FOUND-SW
                 MOVE  WS-SUB-PACK-PROD-NO (SUB-SEARCH) TO
                        WS-PARENT-OF-COMP
NK1209           MOVE  WS-SUB-PACK-QTY (SUB-SEARCH) TO
NK1209                  WS-PARENT-OF-COMP-QTY
              ELSE
                 IF WS-SUB-PACK-PROD-NO(SUB-SEARCH) = SPACES
                     MOVE 'Y'            TO WS-NO-MORE-SEARCH-SW
                 END-IF
              END-IF
           END-PERFORM.
       4000-EXIT.
           EXIT.
      *
       8000-PROCESS-RETURN.
           EXEC CICS RETURN END-EXEC.
           GOBACK.
       8000-EXIT.
           EXIT.
      *
