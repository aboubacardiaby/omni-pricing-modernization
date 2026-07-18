      ******************************************************************
      * COBOL DECLARATION FOR TABLE CU_CID_FREIGHT_FLAG                *
      ******************************************************************
       01 CU-CID-FREIGHT-FLAG.                                          
          05 CUCID-CUSTOMER-NBR        PIC S9(10) COMP-3.          
          05 CUCID-D-EFF-DATE          PIC X(10).                     
          05 CUCID-D-EXP-DATE          PIC X(10).                     
          05 CUCID-F-FRT-FLAG          PIC X(1).                      
          05 CUCID-F-GRP-SANC          PIC X(1).
          05 CUCID-F-GRP-NON-SANC      PIC X(1).
          05 CUCID-F-IND               PIC X(1).
          05 CUCID-F-NON-CONT          PIC X(1).
          05 CUCID-F-CUSTOM            PIC X(1).
          05 CUCID-I-BUY-GROUP         PIC S9(8)
                                         VALUE ZERO COMPUTATIONAL.

       01 CUCID-NULL-INDICATORS.                                     
          05 CUCID-CU-AC-FREIGHT-FLAG-NI.                              
             10 CUCID-D-EXP-DATE-NI    PIC S9(4)                      
                                 VALUE ZERO COMPUTATIONAL.       