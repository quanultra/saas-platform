#!/usr/bin/env python3
"""Fix the Mermaid diagram syntax in component-diagrams.md"""

# Read the file
with open('docs/architecture/component-diagrams.md', 'r') as f:
    lines = f.readlines()

# Find and fix the broken section (around line 63-67)
fixed_lines = []
i = 0
while i < len(lines):
    line = lines[i]

    # Fix the broken "ECS --> R" line
    if line.strip() == 'ECS --> R':
        # Replace with correct connections
        fixed_lines.append('    ECS --> EC\n')
        fixed_lines.append('    EKS --> EC\n')
        fixed_lines.append('    Lambda --> EC\n')
        fixed_lines.append('    ECS --> S3\n')
        fixed_lines.append('    EKS --> S3\n')
        fixed_lines.append('    Lambda --> S3\n')
        fixed_lines.append('    Aurora --> S3\n')
        fixed_lines.append('    DDB --> S3\n')
        fixed_lines.append('\n')

        # Skip the broken continuation line
        i += 1
        if i < len(lines) and '_Aurora[Aurora Active]' in lines[i]:
            # Add proper subgraph definitions
            fixed_lines.append('    subgraph "Primary Region"\n')
            fixed_lines.append('        P_VPC[VPC Primary]\n')
            fixed_lines.append('        P_Aurora[Aurora Active]\n')
            fixed_lines.append('        P_S3[S3 Primary]\n')
            fixed_lines.append('    end\n')
            fixed_lines.append('\n')
            fixed_lines.append('    subgraph "Disaster Recovery"\n')
            fixed_lines.append('        DR_VPC[VPC DR]\n')
            fixed_lines.append('        DR_Aurora[Aurora Standby]\n')
            fixed_lines.append('        DR_S3[S3 DR]\n')
            fixed_lines.append('    end\n')
            fixed_lines.append('\n')
            fixed_lines.append('    subgraph "Warm Standby"\n')
            fixed_lines.append('        WS_VPC[VPC Warm]\n')

            # Skip the malformed lines until we hit "end"
            while i < len(lines) and lines[i].strip() != 'end':
                i += 1

            # Add the rest of Warm Standby subgraph
            fixed_lines.append('        WS_Aurora[Aurora Active]\n')
            fixed_lines.append('        WS_S3[S3 Active]\n')
            fixed_lines.append('        WS_Compute[Compute Scaled Down]\n')
            fixed_lines.append('    end\n')

            # Skip the "end" line as we already added it
            if i < len(lines) and lines[i].strip() == 'end':
                i += 1
        i += 1
        continue

    fixed_lines.append(line)
    i += 1

# Write the fixed content
with open('docs/architecture/component-diagrams.md', 'w') as f:
    f.writelines(fixed_lines)

print("Fixed the Mermaid diagram syntax!")
