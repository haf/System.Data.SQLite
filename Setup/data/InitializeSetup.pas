{
  InitializeSetup.iss --

  Written by Joe Mistachkin.
  Released to the public domain, use at your own risk!
}

function InitializeSetup(): Boolean;
begin
  Result := NetFxInitializeSetup();
end;
