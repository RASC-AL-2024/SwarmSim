import pandas as pd
import matplotlib.pyplot as plt

import os

script_dir = os.path.dirname(__file__)
os.chdir(script_dir)

df = pd.read_csv('../resourceData.csv')

fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(10, 10))

ax1.plot(df['time'], df['battery'], label='Battery', color='blue')
ax1.set_title('Battery Capacity Over Time')
ax1.set_xlabel('Time')
ax1.set_ylabel('Battery')
ax1.grid(True)
ax1.legend()

def calculate_positive_deltas(values):
    running_totals = [0]
    current_total = 0

    for i in range(1, len(values)):
        delta = values[i] - values[i - 1]
        if delta > 0:
            current_total += delta
        running_totals.append(current_total)

    return running_totals

total_spares = calculate_positive_deltas(df['spareModules'])

ax2.plot(df['time'], total_spares, label='Spare Modules', color='green')
ax2.set_title('Spare Modules Over Time')
ax2.set_xlabel('Time')
ax2.set_ylabel('Spare Modules')
ax2.grid(True)
ax2.legend()

plt.tight_layout()
plt.show()