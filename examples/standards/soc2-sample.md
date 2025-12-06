# SOC 2 Trust Services Criteria - Sample

**Standard:** AICPA Trust Services Criteria (SOC 2)
**Version:** 2017
**Description:** Security, Availability, Processing Integrity, Confidentiality, and Privacy

This document contains selected SOC 2 criteria for demonstration purposes.

## CC6.0 - Logical and Physical Access Controls

### CC6.1: Logical and physical access controls

The entity implements logical access security software, infrastructure, and architectures over protected information assets to protect them from security events to meet the entity's objectives.

**Points of Focus:**
- Identifies and manages the inventory of information assets
- Restricts logical access based on user access requirements
- Identifies and authenticates users
- Considers network segmentation
- Manages access credentials
- Uses encryption to protect data
- Protects encryption keys

**Common Criteria:**
- Access control lists (ACLs) are maintained
- Multi-factor authentication for sensitive systems
- Regular access reviews conducted
- Encryption of data at rest and in transit
- Key management procedures documented

### CC6.2: Prior to issuing system credentials and granting system access, the entity registers and authorizes new internal and external users

**Points of Focus:**
- Establishes user access requirements
- Registration and authorization process exists
- Credentials are removed when access is no longer required
- Segregation of duties considerations

**Common Criteria:**
- Formal access request and approval process
- Manager approval required for access grants
- Background checks for employees with privileged access
- Timely account termination process

### CC6.3: The entity authorizes, modifies, or removes access to data, software, functions, and other protected information assets

**Points of Focus:**
- Authorization mechanisms exist and are enforced
- Access rights are reviewed and approved
- Access modifications are tracked and logged
- Emergency access procedures defined

**Common Criteria:**
- Role-based access control (RBAC) implemented
- Quarterly access reviews performed
- Access change logging and monitoring
- Emergency access break-glass procedures

### CC6.6: The entity implements logical access security measures to protect against threats from sources outside its system boundaries

**Points of Focus:**
- Network security measures (firewalls, IDS/IPS)
- Protection against malicious code
- Vulnerability management program
- Network traffic monitoring
- Security of data transmission

**Common Criteria:**
- Firewall rules reviewed quarterly
- Antivirus/anti-malware deployed and updated
- Regular vulnerability scanning
- Penetration testing performed annually
- TLS/SSL for data transmission

### CC6.7: The entity restricts the transmission, movement, and removal of information

**Points of Focus:**
- Data loss prevention (DLP) measures
- Protection of backup media
- Secure data disposal procedures
- Mobile device management
- Removable media controls

**Common Criteria:**
- Email and web filtering implemented
- Encrypted backups stored offsite
- Certified destruction of media
- MDM solution for company devices
- USB port restrictions on workstations

### CC6.8: The entity implements controls to prevent or detect and act upon the introduction of unauthorized or malicious software

**Points of Focus:**
- Malware protection software
- Automatic updates and signature updates
- Scanning of files and email attachments
- Behavior-based detection
- Whitelisting/blacklisting applications

**Common Criteria:**
- Enterprise antivirus solution deployed
- Real-time scanning enabled
- Daily signature updates
- Email attachment scanning
- Application control policies

## CC7.0 - System Operations

### CC7.1: The entity ensures system operations are conducted in a secure manner

**Points of Focus:**
- Change management procedures
- Separation of production and non-production
- Monitoring of system capacity
- Backup and restoration procedures
- Disaster recovery capabilities

**Common Criteria:**
- Formal change management process
- Development, testing, and production environments separated
- Capacity monitoring and planning
- Daily backups with periodic restoration testing
- Documented and tested disaster recovery plan

### CC7.2: The entity identifies, selects, and develops risk mitigation activities arising from potential business disruptions

**Points of Focus:**
- Business continuity planning
- Incident response procedures
- Recovery time and point objectives defined
- Regular testing of continuity plans

**Common Criteria:**
- Business impact analysis conducted
- Incident response plan documented
- RTO and RPO defined for critical systems
- Annual disaster recovery test

### CC7.3: The entity evaluates security events to determine whether they could impact the system

**Points of Focus:**
- Security event detection tools
- Log aggregation and correlation
- Alert generation and response
- Security information and event management (SIEM)

**Common Criteria:**
- Centralized logging infrastructure
- SIEM solution deployed
- Security alerts monitored 24/7
- Security incident investigation procedures

## CC8.0 - Change Management

### CC8.1: The entity authorizes, designs, develops, configures, documents, tests, approves, and implements changes to infrastructure, data, software, and procedures

**Points of Focus:**
- Change management policy and procedures
- Change request and approval process
- Impact assessment for changes
- Testing before production deployment
- Change documentation and communication
- Emergency change procedures
- Rollback procedures

**Common Criteria:**
- Ticketing system for change requests
- Change advisory board (CAB) approval
- Staging environment for testing
- Automated deployment where possible
- Change calendar maintained
- Post-implementation reviews

## A1.0 - Availability

### A1.2: The entity authorizes, designs, develops, implements, operates, approves, maintains, and monitors environmental protections

**Points of Focus:**
- Environmental protection mechanisms
- Temperature and humidity controls
- Fire detection and suppression
- Power supply and backup
- Equipment maintenance

**Common Criteria:**
- Climate-controlled data center
- Fire suppression system
- UPS and generator backup power
- Regular HVAC maintenance
- Environmental monitoring systems

### A1.3: The entity authorizes, designs, develops, implements, operates, approves, maintains, and monitors detective and preventative measures designed to identify and protect from malicious software

**Points of Focus:**
- Antivirus and anti-malware protection
- Regular signature updates
- Scanning procedures
- Malware incident response
- User awareness training

**Common Criteria:**
- Enterprise-grade endpoint protection
- Automatic signature updates
- Full system scans weekly
- Malware incident playbook
- Annual security awareness training

## C1.0 - Confidentiality

### C1.1: The entity identifies and maintains confidential information

**Points of Focus:**
- Data classification policy
- Identification of confidential data
- Labeling and handling procedures
- Storage and transmission security
- Disposal of confidential information

**Common Criteria:**
- Data classification framework (Public, Internal, Confidential)
- Confidential data inventory maintained
- Encryption of confidential data
- Secure file transfer solutions
- Shredding/wiping procedures for disposal

### C1.2: The entity disposes of confidential information to meet the entity's objectives

**Points of Focus:**
- Disposal policies and procedures
- Secure destruction methods
- Disposal tracking and documentation
- Vendor disposal procedures
- Verification of disposal

**Common Criteria:**
- Data disposal policy documented
- Certified shredding service used
- Certificates of destruction retained
- Vendor contracts include disposal requirements
- IT asset disposal procedures
