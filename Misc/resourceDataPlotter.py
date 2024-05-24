import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from scipy.signal import savgol_filter
import os

def smooth(x):
  return x # savgol_filter(x, min(len(x), 51), 3)

df = pd.read_csv(os.path.dirname(__file__) + '/../resourceData.csv')

fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(10, 10))

ax1.plot(df['time'], df['battery'], label='Battery', color='blue')
ax1.set_title('Battery Capacity Over Time')
ax1.set_xlabel('Time')
ax1.set_ylabel('Battery')
ax1.grid(True)
ax1.legend()

ax2.plot(df['time'], df['totalDirt'], label='Total Ore', color='green')
ax2.set_title('Total Ore Collected Over Time')
ax2.set_xlabel('Time (s)')
ax2.set_ylabel('Ore mass (g)')
ax2.grid(True)
ax2.legend()

generated = df['totalGenerated']
drained = generated - df['battery']
dt = np.diff(df['time'], prepend=0)

ax3.plot(df['time'], np.maximum(0, smooth(np.diff(generated, prepend=0) / dt)), label='Energy Generated')
ax3.plot(df['time'], smooth(np.maximum(0, np.diff(drained, prepend=0) / dt)), label='Energy Consumed')
ax3.set_title('Energy Flow Over Time')
ax3.set_xlabel('Time (s)')
ax3.set_ylabel('W')
ax3.grid(True)
ax3.legend()

ax4.plot(df['time'], df['totalModules'], label='Total Modules', color='green')
ax4.set_title('Total Modules Produced Over Time')
ax4.set_xlabel('Time (s)')
ax4.set_ylabel('Modules Produced')
ax4.grid(True)
ax4.legend()

plt.tight_layout()
plt.show()
