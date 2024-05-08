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

ax2.plot(df['time'], df['totalDirt'], label='Total Dirt', color='green')
ax2.set_title('Total Dirt Over Time')
ax2.set_xlabel('Time')
ax2.set_ylabel('Total Dirt')
ax2.grid(True)
ax2.legend()

plt.tight_layout()
plt.show()