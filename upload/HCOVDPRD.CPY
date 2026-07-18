      *----------------------------------------------------------------
      *    08/16/21 SJACOB   IR 20-0065 PRICER ENHANCEMENT PHASE 2     
      *                      COPYBOOK FOR TABLE HC_OVRD_PRODUCT        
      *                      INITIAL CREATION                          
      *----------------------------------------------------------------
       01 HC-OVD-PRODUCT.                                              
          05 HC-PROD-GROUP-ID          PIC S9(8) VALUE 0 COMPUTATIONAL.
          05 HC-PROD-VENDOR            PIC X(04) VALUE SPACE.          
          05 HC-PROD-PRODUCT           PIC X(08) VALUE SPACE.          
          05 HC-PROD-CST-START-DATE    PIC X(23) VALUE SPACE.          
          05 HC-PROD-CST-EXPIRE-DATE   PIC X(23) VALUE SPACE.          
          05 HC-PROD-SELL-START-DATE   PIC X(23) VALUE SPACE.          
          05 HC-PROD-SELL-EXPIRE-DATE  PIC X(23) VALUE SPACE.          
          05 HC-PROD-OVRD-FLAG         PIC X(01) VALUE SPACE.          
          05 HC-PROD-OVRD-TYPE         PIC X(06) VALUE SPACE.          
          05 HC-PROD-OVRD-PERCENT      PIC S9(1)V9(4) VALUE 0 COMP-3.  
          05 HC-PROD-OVRD-SELL-PRC     PIC S9(7)V9(4) VALUE 0 COMP-3.  
          05 HC-PROD-SELL-UM           PIC X(02) VALUE SPACE.          
          05 HC-PROD-USER-ID           PIC X(08) VALUE SPACE.
                                                                       
       01  HC-PROD-NULL-INDICATORS.                                   
              10 HC-PROD-CST-EXP-DATE-NI                               
                                       PIC S9(4) VALUE 0 COMPUTATIONAL.
              10 HC-PROD-SELL-EXP-DATE-NI                              
                                       PIC S9(4) VALUE 0 COMPUTATIONAL.