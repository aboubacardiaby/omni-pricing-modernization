SN0226*----------------------------------------------------------------*
SN0226* 02/19/26 SNIGAM2   PRJ0015313 CUSTOMER SERVICE FLAG            *
SN0226*                    (Extension of Hide UOM project)             *
SN0226*                    Added new field CUM-UUS-SERVICE-FLAG        *
SN0226*                    Indicates whether or not to apply the UUS   *
sn0226*                    related logic for SO creation.              *
SN0226*----------------------------------------------------------------*
KS0723*-------------------------------------------------------------
KS0723* 07/2023  KSHEKA    IR 23-0023 LIMITING SALEABLE UOMS PHASE 2
KS0723*
KS0723*   ADDED NEW FIELD CUM-LOGUNIT-EX-IN-SW
KS1119*-------------------------------------------------------------
KS1119* 11/2019  KSHEKA    IR 19-0071 SPLIT INVOICING          
KS1119*                                               
KS1119*   DELETED CUM-PRICE-ASSURANCE-SW 
KS1119*   ADDED NEW FIELDS CUM-SPLIT-BILL-SW & CUM-SPLIT-FORCE-DAYS
KS0418*-------------------------------------------------------------
KS0418* 04/2018  KSHEKA    CR 18-0017 SPLIT INVOICING FOR PRICE
KS0418*                               DISCREPANCIES
KS0418*   RENAMED CUM-BILLING-HOLD TO CUM-PRICE-ASSURANCE-SW
NB0501*-------------------------------------------------------------
NB0501* 05/01/17 NBASAR    CR 17-0033 CMF Billing Hold Flag 
NB0501*
NB0501*   ADD NEW FIELD TO CUTMST , BILLING HOLD FLAG, BILLING_HOLD   
NB0501*         
NB0501*   THIS WILL GIVE THE BUSINESS THE ABILLITY TO PUT ORDERS
NB0501*   FOR THE ENTIRE ACCOUNT ON BILLING HOLD TO PREVENT 
NB0501*   THEM FROM BILLIING 
NB0501*          
NB0501*   CUW500-PARM-LENGTH CHANGED FROM 941 TO 947.
NB0501* --------------------------------------------------------------
JN0910******************************************************************
DWB003* 01/21/13  WBURKE      OMMARKETPLACE
DWB003*                       ADD NEW COLUMNS PO-GENER-TYPE AND
DWB003*                       GENERATED-PO
VN0912* 09/12/12  VNULE       CR#11-0093  ADD NEW SPLIT-SVC-FEES-SW
      *                                   COLUMN SWITCH
JN0910* 09/11/10  JNOLAN      OWENM93223  JNOLAN  ADD NEW GLN SWITCH
      *
      * 07/08/08  WBURKE      CR# 07-0153 - ADD NEW SI-DAY-OF-MONTH
      *                       COLUMN AND SI-MONTH-LAST-RUN
      * 12/08/06  JMONSO      CR# 06-0071 - ADD NEW FUTRE-PROM-DT-DAYS
      *                       COLUMN
      * 02/02/05  S. BODNAR   CR# 05-0003 - ADD NEW ADMIN-HOLD-REAS-
      *SB0205                 CD COLUMN
*     * CR #03-0059   JNOLAN             12/09/2003
*     *          ADDED 88 LEVELS CUM-SI-PRINT-DETAIL-BY-DEPT
*     *          AND CUM-SI-PRINT-BOTH-BY-DEPT
*     * CR #03-0059   JNOLAN             11/20/2003
*     *          ADDED 88 LEVEL CUM-SI-PRINT-BOTH
*     * CR #1290      MRAYMO  CMSTR4     03/01/2003
*     *          ADDED ATTRIBUTES FOR CMAST R4
*     * CR #1290      BENNY WOODLIEF     06/01/2002
*     *          ADDED ATTRIBUTES FOR CMAST PROJECT
      *-------------------------------------------
      * CUSTOMER MASTER TABLE
      *-------------------------------------------
      *
       01  CUM-COMPANY-ID              PIC X(02).
       01  CUM-CUST-ID                 PIC X(15).
       01  CUM-STATUS                  PIC X(01).
CR1290     88 CUM-ACTIVE-CUST                          VALUE ' '.
           88 CUM-DELETE                               VALUE '*'.
           88 CUM-NON-SALEABLE                         VALUE 'N'.
CR1290     88 CUM-PENDING-CUST                         VALUE 'P'.
       01  CUM-DEF-SHIP-SUF            PIC X(04).
       01  CUM-DEF-BILL-SUF            PIC X(04).
       01  CUM-CAPS-ID                 PIC X(04).
       01  CUM-LOOKUP-ID               PIC X(20).
       01  CUM-PRICE-CUST-ID           PIC X(15).
       01  CUM-CUST-TYPE               PIC X(02).
           88 CUM-TYPE-TRANSFER                        VALUE 'T '.
           88 CUM-TYPE-CASH                            VALUE 'Y '.
       01  CUM-CUST-PO-SW              PIC X(01).
           88 CUM-CUST-PO-REQUIRED                     VALUE 'Y'.
       01  CUM-BACKORDER-CD            PIC X(01).
           88 CUM-BACKORDER                            VALUE 'Y'.
           88 CUM-SHIP-COMPLETE                        VALUE 'C'.
           88 CUM-DO-NOT-BACKORDER                     VALUE 'N'.
       01  CUM-ACK-CD                  PIC X(01).
           88 CUM-ACK-FAX                              VALUE 'F'.
           88 CUM-ACK-EMAIL                            VALUE 'M'.
           88 CUM-ACK-PRINT                            VALUE 'P'.
           88 CUM-NO-ACK                               VALUE 'N'.
           88 CUM-ACK-REQ                              VALUE 'F' 'M'
                                                              'P'.
       01  CUM-COPY-CD                 PIC X(01).
           88 CUM-COPY-FAX                             VALUE 'F'.
           88 CUM-COPY-EMAIL                           VALUE 'M'.
           88 CUM-COPY-EXPORT                          VALUE 'E'.
           88 CUM-COPY-MAIL-TO-BR                      VALUE 'B'.
           88 CUM-COPY-DO-NOT-PRINT                    VALUE 'N'.
       01  CUM-EXT-SO-CD               PIC X(01).
           88 CUM-EXT-SO                               VALUE 'Y'.
       01  CUM-REPORT-CD               PIC X(01).
           88 CUM-DAILY-NEW-ORD-REQ                    VALUE 'D'.
           88 CUM-WKLY-NEW-ORD-REQ                     VALUE 'W'.
       01  CUM-PRIORITY-CD             PIC X(01).
           88 CUM-HIGHEST-PRIORITY                     VALUE '1'.
           88 CUM-DEFAULT-PRIORITY                     VALUE '7'.
           88 CUM-LOWEST-PRIORITY                      VALUE '9'.
       01  CUM-SI-CD                   PIC X(1).
           88 CUM-SI-BILL-OF-LADING                    VALUE 'P'.
           88 CUM-SI-DAILY-INVOICE                     VALUE 'D'.
           88 CUM-BIWEEKLY-INVOICE                     VALUE 'B'.
           88 CUM-MONTHLY-INVOICE                      VALUE 'M'.
           88 CUM-WEEKLY-INVOICE                       VALUE 'W'.
       01  CUM-SI-DET-RPT-CD           PIC X(1).
           88 CUM-SI-PRINT-DETAIL                      VALUE 'D'.
           88 CUM-SI-PRINT-SUMMARY                     VALUE 'S'.
           88 CUM-SI-PRINT-BOTH                        VALUE 'B'.
           88 CUM-SI-NO-PRINT                          VALUE 'N' ' '.
           88 CUM-SI-PRINT-DETAIL-BY-DEPT              VALUE 'T'.
           88 CUM-SI-PRINT-BOTH-BY-DEPT                VALUE 'P'.
       01  CUM-STMT-PRINT-CD           PIC X(1).
           88 CUM-NO-STMT                              VALUE 'N'.
           88 CUM-STMT-EMAIL                           VALUE 'M'.
           88 CUM-STMT-FAX                             VALUE 'F'.
           88 CUM-STMT-PRINT                           VALUE 'Y'.
           88 CUM-STMT                                 VALUE 'Y' 'M'
                                                              'F'.
       01  CUM-CREATE-DT               PIC X(10).
       01  CUM-STD-IND-CD              PIC X(4).
       01  CUM-NOTES-SW                PIC X(1).
           88 CUM-NOTES                                VALUE 'Y'.
       01  CUM-NAME                    PIC X(35).
       01  CUM-SHIP-DOC-SEQ            PIC X(01).
           88 CUM-PALLET-NBR-SEQ                       VALUE 'P'.
           88 CUM-PO-NBR-SEQ                           VALUE 'C'.
       01  CUM-SUBSTITUTE-SW           PIC X(01).
           88 CUM-SUBSTITUTE                           VALUE 'Y' ' '.
           88 CUM-DO-NOT-SUBSTITUTE                    VALUE 'N'.
       01  CUM-UPDATE-STAMP            PIC X(26).
       01  CUM-NATIONAL-ACCT           PIC X(15).
       01  CUM-PPD-FRT-AMT             PIC S9(7)       COMP-3.
       01  CUM-CUST-CLS                PIC X(02).
       01  CUM-MIN-ORDER-AMT           PIC S9(7)       COMP-3.
       01  CUM-INVC-CUST-ID            PIC X(15).
       01  CUM-INVC-CUST-SUF           PIC X(04).
       01  CUM-SLS-ACCT-ID.
           49 CUM-SLS-ACCT-ID-L        PIC S9(4)       COMP.
           49 CUM-SLS-ACCT-ID-T        PIC X(35).
       01  CUM-CGS-ACCT-ID.
           49 CUM-CGS-ACCT-ID-L        PIC S9(4)       COMP.
           49 CUM-CGS-ACCT-ID-T        PIC X(35).
       01  CUM-AR-ACCT-ID.
           49 CUM-AR-ACCT-ID-L         PIC S9(4)       COMP.
           49 CUM-AR-ACCT-ID-T         PIC X(35).
       01  CUM-CUSTOM1.
           49 CUM-CUSTOM1-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM1-T            PIC X(35).
       01  CUM-CUSTOM2.
           49 CUM-CUSTOM2-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM2-T            PIC X(35).
       01  CUM-CUSTOM3.
           49 CUM-CUSTOM3-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM3-T            PIC X(35).
       01  CUM-CUSTOM4.
           49 CUM-CUSTOM4-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM4-T            PIC X(35).
       01  CUM-CUSTOM5.
           49 CUM-CUSTOM5-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM5-T            PIC X(35).
       01  CUM-CUSTOM6.
           49 CUM-CUSTOM6-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM6-T            PIC X(35).
       01  CUM-CUSTOM7.
           49 CUM-CUSTOM7-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM7-T            PIC X(35).
       01  CUM-CUSTOM8.
           49 CUM-CUSTOM8-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM8-T            PIC X(35).
       01  CUM-CUSTOM9.
           49 CUM-CUSTOM9-L            PIC S9(4)       COMP.
           49 CUM-CUSTOM9-T            PIC X(35).
       01  CUM-CUSTOM10.
           49 CUM-CUSTOM10-L           PIC S9(4)       COMP.
           49 CUM-CUSTOM10-T           PIC X(35).
       01  CUM-CUSTOM11                PIC S9(9)V9(4)  COMP-3.
       01  CUM-CUSTOM12                PIC S9(9)V9(4)  COMP-3.
       01  CUM-CUSTOM13                PIC S9(9)V9(4)  COMP-3.
       01  CUM-CUSTOM14                PIC S9(9)V9(4)  COMP-3.
       01  CUM-CUSTOM15                PIC S9(9)V9(4)  COMP-3.
       01  CUM-EDI-ERROR-SW            PIC X(1).
           88 CUM-SPLIT-EDI-ERRORS                     VALUE 'Y'.
           88 CUM-DO-NOT-SPLIT-EDI-ERRORS              VALUE 'N'.
       01  CUM-OTB-ORDER-SW            PIC X(01).
           88 CUM-OPEN-TO-BUY-ORDERS-EXIST             VALUE 'Y'.
       01  CUM-ADDR-VER-SW             PIC X(01).
           88 CUM-BYPASS-ADDR-VER                      VALUE 'Y'.
       01  CUM-RA-PRINT-CD             PIC X(01).
           88 CUM-RA-FAX                               VALUE 'F'.
           88 CUM-RA-EMAIL                             VALUE 'M'.
           88 CUM-RA-PRINT                             VALUE 'P'.
           88 CUM-NO-RA-PRINT                          VALUE 'N'.
       01  CUM-SHPNOTICE-CD            PIC X(01).
       01  CUM-SHIP-NOTICE-CD
                                       REDEFINES  CUM-SHPNOTICE-CD
                                       PIC X(01).
           88 CUM-SHIP-NOTICE-FAX                      VALUE 'F'.
           88 CUM-SHIP-NOTICE-EMAIL                    VALUE 'M'.
           88 CUM-SHIP-NOTICE-NONE                     VALUE 'N'.
       01  CUM-DEBIT-MEMO-CD           PIC X(01).
           88 CUM-DEBIT-MEMO-FAX                       VALUE 'F'.
           88 CUM-DEBIT-MEMO-EMAIL                     VALUE 'M'.
           88 CUM-DEBIT-MEMO-PRINT                     VALUE 'P'.
           88 CUM-NO-DB-PRINT                          VALUE 'N'.
       01  CUM-EXPEDITE-DAYS           PIC S9(3)       COMP-3.
       01  CUM-EXPEDITE-SW             PIC X(01).
           88 CUM-EXPEDITE                             VALUE 'Y'.
           88 CUM-DO-NOT-EXPEDITE                      VALUE 'N'.
       01  CUM-PRICE-OPT-SW            PIC X(01).
           88 CUM-PRICE-AT-CONSUMPTION                 VALUE 'C'.
           88 CUM-PRICE-AT-CONSIGN                     VALUE 'O'.
CMAST  01  CUM-OMD-STATUS              PIC X(01).
CMAST      88 CUM-OMDIRECT-ACTIVE                      VALUE 'Y'.
CMAST      88 CUM-OMDIRECT-INACTIVE                    VALUE 'N'.
CMAST  01  CUM-DODAC-NBR               PIC X(06).
CMAST  01  CUM-DODAAC-NBR              PIC X(08).
CMAST  01  CUM-DEA-NUMBER              PIC X(09).
CMAST  01  CUM-PART-XREF-NBR           PIC X(08).
CHIER  01  CUM-CUSTOMER-NBR            PIC S9(10)      COMP-3.
CHIER  01  CUM-DEF-IS-SLSM-ID          PIC X(06).
CHIER  01  CUM-DEF-OS-SLSM-ID          PIC X(06).
CHIER  01  CUM-DEF-OMS-SLSM-ID         PIC X(06).
CHIER  01  CUM-DEF-LS-SLSM-ID          PIC X(06).
CHIER  01  CUM-DEF-COST-TRACK-FLG      PIC X(01).
CHIER  01  CUM-DEF-PANDAC-FLAG         PIC X(01).
CHIER  01  CUM-STATUS-DATE             PIC X(10).
CHIER  01  CUM-WISDOM-FLAG             PIC X(01).
CHIER  01  CUM-CT-FEE-BILLED-ACCT      PIC X(15).
SBODNA 01  CUM-PROFIT-ORD-HI           PIC S9(03) COMP-3.
SBODNA 01  CUM-PROFIT-ORD-LO           PIC S9(03) COMP-3.
SBODNA 01  CUM-PROFIT-LNE-HI           PIC S9(03) COMP-3.
SBODNA 01  CUM-PROFIT-LNE-LO           PIC S9(03) COMP-3.
CMSTR4 01  CUM-EXPORT-EMAIL-ADDR.
CMSTR4     49 CUM-EXPORT-EMAIL-ADDR-L  PIC S9(4)  COMP.
CMSTR4     49 CUM-EXPORT-EMAIL-ADDR-T  PIC X(50).
CMSTR4 01  CUM-EXPORT-REQUISITION      PIC X(01).
CMSTR4 01  CUM-HANDHELD-ACCOUNT        PIC X(01).
CMSTR4     88 CUM-HANDHELD-ACTIVE                      VALUE 'Y'.
CMSTR4     88 CUM-HANDHELD-INACTIVE                    VALUE 'N'.
CMSTR4 01  CUM-SUM-PANDAC-FLAG         PIC X(01).
CMSTR4 01  CUM-SUM-PANDAC-START        PIC X(10).
CMSTR4 01  CUM-SUM-PANDAC-END          PIC X(10).
CMSTR4 01  CUM-NAME-IN-PER-REPORT.
CMSTR4     49 CUM-NAME-IN-PER-REPORT-L PIC S9(4) COMP.
CMSTR4     49 CUM-NAME-IN-PER-REPORT-T PIC X(35).
SB0205 01  CUM-ADMIN-HOLD-REAS-CD        PIC X(02).
C06071 01  CUM-FUTR-PROM-DT-DAYS       PIC S9(03) COMP-3.
DWB002 01  CUM-SI-DAY-OF-MONTH         PIC X(02).
DWB002 01  CUM-SI-MONTH-LAST-RUN       PIC X(02).
JN0910 01  CUM-EDI-GLN-SW              PIC X(01).
VN0912 01  CUM-SPLIT-SVC-FEES-SW       PIC X(01).
VN0912     88 SPLIT-FEES                               VALUES 'Y'.
VN0912     88 DO-NOT-SPLIT-FEES                        VALUES 'N' ' '.
DWB003 01  CUM-PO-GENER-TYPE           PIC X(01).
DWB003 01  CUM-GENERATED-PO            PIC X(18).
KS0418*01  CUM-PRICE-ASSURANCE-SW      PIC X(01).
KS0418*    88  CUM-PRICE-ASSURANCE-YES                VALUES 'Y'.
KS0418*    88  CUM-PRICE-ASSURANCE-NO                 VALUES 'N' ' '.
KS1119 01  CUM-SPLIT-BILL-SW           PIC X(01).
KS1119 01  CUM-SPLIT-FORCE-DAYS        PIC 9(03).
KS0723 01  CUM-LOGUNIT-EX-IN-SW        PIC X(01).
KS0723     88  CUM-LOGUNIT-EXCL-LIST   VALUE 'E'.
KS0723     88  CUM-LOGUNIT-INCL-LIST   VALUE 'I'.
SN0326 01  CUM-UUS-SERVICE-SW          PIC X(01) VALUE SPACES.
SN0326     88 CUM-ORDER-IN-UOU-ALLOWED           VALUE 'Y'.
SN0326     88 CUM-ORDER-IN-UOU-NOT-ALLOWED       VALUE 'N' ' '.