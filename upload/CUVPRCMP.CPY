      *:---------------------------------------------------------------*
      *: REVISION SECTION ------------- (MOST RECENT FIRST) -----------*
      *:---------------------------------------------------------------*
      *    DATE   PROGRAMMER         DESCRIPTION OF CHANGE             *
      *:---------------------------------------------------------------*
      *  07/2022  SJACOB  WR 22-9470 : OMNI2 PRICER ENHANCEMENT 2022   *
      *                   NEW COPYBOOK FOR CUTPRICE_COMPONENT TABLE    *
      *----------------------------------------------------------------*
       01 CU-PRICE-COMPONENT-DATA.
          05 CUPC-COMPANY-ID           PIC X(02).
          05 CUPC-CUST-ID              PIC X(15).
          05 CUPC-SHIP-TO-SUFFIX       PIC X(04).
          05 CUPC-COMP-SHORT-CODE      PIC X(02).
          05 CUPC-COMP-NAME            PIC X(30).
          05 CUPC-SKU-CODE             PIC X(04).
          05 CUPC-BILLING-FREQ         PIC X(02).
             88 CUPC-DAILY-BILL            VALUE 'DL'.
             88 CUPC-MONTHLY-AUTO-BILL     VALUE 'MA'.
             88 CUPC-MONTHLY-MANUAL-BILL   VALUE 'MM'.
          05 CUPC-CUST-ORDER-NBR       PIC X(18).
          05 CUPC-DATE-EFFECTIVE       PIC X(10).
          05 CUPC-DATE-EXPIRE          PIC X(10).
       01 CUPC-NULL-INDICATORS.                                       
          10 CUPC-D-ADJ-EXP-NI         PIC S9(4)         
                                           VALUE ZERO COMPUTATIONAL.   
          10 CUPC-NO-NULL-VARIABLES    PIC S9(4)         
                                           VALUE +0001 COMPUTATIONAL.