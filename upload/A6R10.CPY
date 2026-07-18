      ******************************************************************00000001
      * COPY BOOK FOR DATASTRUCTURE USED TO PASS DATA TO & RECEIVE FROM*00000002
      * SUBROUTINE A6U10 & A6U10B                                      *00000003
      * CREATED BY : SATHYA      DDI, FAX: 266-4388                    *00000004
      * CREATION DT: MAR 2003                                          *00000005
      * TOTAL LENGTH AT THE TIME OF INITIAL CREATION: 250 CHARS        *00000006
      *                                                                *00000007
      * NOTE:                                                          *00000008
      * -----                                                          *00000009
      * WHENEVER CHANGE IS MADE TO THIS DATA STRUCTURE , AFFECTING     *00000010
      * THE TOTAL LENGTH OF THIS RECORD, THE FOLLOWING NEED TO BE CHANG*00000011
      * -ED:                                                           *00000012
      * 1) LENGTH OF REC IN LINKAGE SECTION IN A6U10                   *00000013
      * 2) A6U10 TO BE RECOMPILED                                      *00000014
      *                                                                *00000015
      ******************************************************************00000016
      *----------------------------------------------------------------*00000017
      *                    CHANGE HISTORY                              *00000018
      *----------------------------------------------------------------*00000019
      * DATE   PGMR        DESCRIPTION                                 *00000020
      *----------------------------------------------------------------*00000021
          05  A6W10                             PIC X(250).             00000022
          05  A6W10-REDEF  REDEFINES A6W10.                             00000023
            10 A6W10-RECEIVD-DATA.                                      00000024
              15  A6W10-PART-NUMBER.                                    00000025
                 20 A6W10-I-VENDOR             PIC X(04).
                 20 A6W10-I-VND-PRODUCT        PIC X(08).
              15  FILLER                       PIC X(25).               00000026
            10 A6W10-RTND-DATA.                                         00000027
              15  A6W10-PROD-DESC              PIC X(35).               00000028
              15  A6W10-PROD-TYPE              PIC X(01).               00000030
              15  A6W10-PROD-STATUS            PIC X(01).               00000030
              15  A6W10-PROD-EFF-DATE          PIC X(10).               00000030
              15  A6W10-PROD-EXP-DATE          PIC X(10).               00000030
              15  A6W10-GENERAL-RESPONSE-FLAG  PIC X(01).               00000031
                  88 A6W10-ABEND               VALUE 'A'.
                  88 A6W10-ERROR               VALUE 'E'.
                  88 A6W10-WARNING             VALUE 'W'.
              15  A6W10-GENERAL-ERROR-NUMBER   PIC X(05).               00000032
              15  A6W10-ABEND-MSG.                                      00000033
                  25  A6W10-ERROR-MESSAGE      PIC X(80).               00000034
                  25  A6W10-ERROR-SQLCODE      PIC X(05).               00000035
              15  FILLER                       PIC X(65).               00000040
      *                                                                 00000065
