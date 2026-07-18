      *:---------------------------------------------------------------*
      *: REVISION SECTION ------------- (MOST RECENT FIRST) -----------*
      *:---------------------------------------------------------------*
      *    DATE   PROGRAMMER         DESCRIPTION OF CHANGE             *
      *:---------------------------------------------------------------*
      *  07/2022  DPALEK  WR 22-9470 : OMNI2 PRICER ENHANCEMENT 2022   *
      *                   NEW COPYBOOK FOR CUSTOMER FEES OMNI2 TABLE   *
      *----------------------------------------------------------------*
       01 CUFP-COMPANY-ID          PIC X(2).
       01 CUFP-CUST-ID             PIC X(15).
       01 CUFP-SHIP-TO-SUFFIX      PIC X(4).
       01 CUFP-FEE-SHORT-CODE      PIC X(2).
       01 CUFP-FEE-NAME            PIC X(30).
       01 CUFP-FEE-TYPE            PIC X(1).
          88 FEE-PER-LINE         VALUE 'L'.
          88 FEE-PER-QTY          VALUE 'Q'.
          88 FEE-PER-ORDER        VALUE 'O'.
          88 FEE-PCT-SALES        VALUE 'P'.
          88 FEE-PCT-COST         VALUE 'C'.
       01 CUFP-FEE-PCT             PIC S9(11)V9(4) COMP-3.
       01 CUFP-FEE-AMT             PIC S9(5)V9(2) COMP-3.
       01 CUFP-SKU-CODE            PIC X(4).
       01 CUFP-BILLING-FREQ        PIC X(2).
           88 DAILY-BILL          VALUE 'DL'.
           88 DAILY-BILL-SEP      VALUE 'DS'.
           88 MONTHLY-AUTO-BILL   VALUE 'MA'.
           88 MONTHLY-MANUAL-BILL VALUE 'MM'.
       01 CUFP-CUST-ORDER-NBR      PIC X(18).
       01 CUFP-DATE-EFFECTIVE      PIC X(10).
       01 CUFP-DATE-EXPIRE         PIC X(10).
       01 CUFP-LUM-FEE-AMT         PIC S9(5)V9(2) COMP-3.
       01 CUFP-LUM-FEE-PCT         PIC S9(11)V9(4) COMP-3.
       01 CUFP-NULL-INDICATORS.                                       
              10 CUFP-D-ADJ-EXP-NI            PIC S9(4)         
                                           VALUE     ZERO               
                                           COMPUTATIONAL.               
              10 CUFP-NO-NULL-VARIABLES              PIC S9(4)         
                                           VALUE     +0001              
                                           COMPUTATIONAL.               