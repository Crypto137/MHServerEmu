using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.JsonOutput
{
    public class BlueprintJson
    {
        public string RuntimeBinding { get; }
        public string DefaultPrototypeId { get; }
        public BlueprintReferenceJson[] Parents { get; }
        public BlueprintReferenceJson[] ContributingBlueprints { get; }
        public BlueprintMemberJson[] Members { get; }

        public BlueprintJson(Blueprint blueprint)
        {
            RuntimeBinding = blueprint.RuntimeBinding;
            DefaultPrototypeId = GameDatabase.GetPrototypeName(blueprint.DefaultPrototypeId);

            Parents = new BlueprintReferenceJson[blueprint.Parents.Length];
            for (int i = 0; i < Parents.Length; i++)
                Parents[i] = new(blueprint.Parents[i]);

            ContributingBlueprints = new BlueprintReferenceJson[blueprint.ContributingBlueprints.Length];
            for (int i = 0; i < ContributingBlueprints.Length; i++)
                ContributingBlueprints[i] = new(blueprint.ContributingBlueprints[i]);

            Members = new BlueprintMemberJson[blueprint.Members.Length];
            for (int i = 0; i < Members.Length; i++)
                Members[i] = new(blueprint.Members[i]);
        }

        public class BlueprintReferenceJson
        {
            public string Id { get; }
            public byte ByteField { get; }

            public BlueprintReferenceJson(BlueprintReference reference)
            {
                Id = GameDatabase.GetPrototypeName(reference.Id);
                ByteField = reference.ByteField;
            }
        }

        public class BlueprintMemberJson
        {
            public ulong FieldId { get; }
            public string FieldName { get; }
            public char ValueType { get; }
            public char ContainerType { get; }
            public string Subtype { get; }

            public BlueprintMemberJson(BlueprintMember member)
            {
                FieldId = member.FieldId;
                FieldName = member.FieldName;
                ValueType = (char)member.ValueType;
                ContainerType = (char)member.ContainerType;

                switch (ValueType)
                {
                    // Only these types have subtypes
                    case 'A':
                        Subtype = GameDatabase.GetAssetTypeName(member.Subtype);
                        break;

                    case 'C':
                        Subtype = GameDatabase.GetCurveName(member.Subtype);
                        break;

                    // Both P and R have prototypes as their subtypes
                    case 'P':
                    case 'R':
                        Subtype = GameDatabase.GetPrototypeName(member.Subtype);
                        break;
                }
            }
        }
    }
}