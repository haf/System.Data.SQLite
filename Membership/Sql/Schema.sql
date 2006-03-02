Create Table Application(
  AppID  INTEGER PRIMARY KEY,
  ApplicationName text
);

Create Table Role(
	RoleID INTEGER PRIMARY KEY,
	Rolename text,
	AppID integer NOT NULL
);

Create Table UserRoleMap(
	UserID integer NOT NULL,
	RoleID integer NOT NULL,
	AppID integer NOT NULL
);
	
CREATE TABLE User
(
  UserID INTEGER PRIMARY KEY,
  Username text NOT NULL,
  AppID integer NOT NULL,
  Email text  NOT NULL,
  Comment text,
  Password text NOT NULL,
  PasswordQuestion text,
  PasswordAnswer text,
  IsApproved bool, 
  LastActivityDate DateTime,
  LastLoginDate DateTime,
  LastPasswordChangedDate DateTime,
  CreationDate DateTime, 
  IsOnLine bool,
  IsLockedOut bool,
  LastLockedOutDate DateTime,
  FailedPasswordAttemptCount integer,
  FailedPasswordAttemptWindowStart DateTime,
  FailedPasswordAnswerAttemptCount integer,
  FailedPasswordAnswerAttemptWindowStart DateTime
);

Create Table SiteMapNode(
	NodeID	INTEGER PRIMARY KEY,
	AppID integer NOT NULL,
	Title text,
	Description text,
	Url text,
	Parent integer
);

Create Table SiteMapNodeRoles(
	NodeID integer NOT NULL,
	RoleID integer NOT NULL,
	AppID integer NOT NULL
);


Create Table Profile(
	ProfileID INTEGER PRIMARY KEY,
	UserName text NOT NULL,
	AppID integer NOT NULL,
	LastUpdatedDate Datetime,
	LastActivityDate Datetime
);

Create Table ProfileData(
	ItemID INTEGER PRIMARY KEY,
	ProfileID integer NOT NULL,
	ItemData BLOB,
	ItemName text NOT NULL,
	ItemFormat text NOT NULL
);
